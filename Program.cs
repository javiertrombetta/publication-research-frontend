using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Http;
using ResearchPublicationManagementSystem.Infrastructure.Options;
using ResearchPublicationManagementSystem.Services;
using ResearchPublicationManagementSystem.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Most PaaS targets (Render included) assign the listen port at runtime via PORT rather than a
// fixed value baked into config. Only override Kestrel's URLs when it is actually set, so local
// development keeps using launchSettings.json as before.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ---------- Options ----------
builder.Services.AddOptions<ApiOptions>().Bind(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.AddOptions<InstitutionOptions>().Bind(builder.Configuration.GetSection(InstitutionOptions.SectionName));

// ---------- Cookie authentication (holds the backend JWT access/refresh tokens as claims) ----------
var authenticationBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/home";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ---------- Signing in with an institutional Microsoft account ----------
//
// Registered only where the deployment has been given a tenant, an application and the API's scope.
// Where it has not, none of this exists: no scheme, no redirect, no button, and the site signs
// people in with a password exactly as before.
//
// The Microsoft sign-in does not become the session. It ends in a short-lived cookie of its own
// that carries the token Entra issued, which AuthController trades with the API for this
// application's own tokens, and it is those that become the session. So a person who arrives this
// way holds the same claims, roles and refresh token as one who typed a password, and everything
// downstream is unaware there was ever a difference.
var microsoftSso = builder.Configuration.GetSection(MicrosoftSsoOptions.SectionName)
    .Get<MicrosoftSsoOptions>() ?? new MicrosoftSsoOptions();

builder.Services.AddSingleton(microsoftSso);

if (microsoftSso.IsConfigured)
{
    authenticationBuilder
        .AddCookie(MicrosoftSso.HandoverScheme, options =>
        {
            // Alive only for the moment between coming back from Microsoft and being signed in
            // here. It is not a session and must never be mistaken for one.
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        })
        .AddOpenIdConnect(MicrosoftSso.Scheme, options =>
        {
            options.SignInScheme = MicrosoftSso.HandoverScheme;
            options.Authority = microsoftSso.Authority;
            options.ClientId = microsoftSso.ClientId;
            options.ClientSecret = microsoftSso.ClientSecret;

            // Authorisation code flow. The implicit alternatives hand tokens to the browser, and
            // the token this needs is one the server uses, not the browser.
            options.ResponseType = "code";
            options.UsePkce = true;
            options.SaveTokens = true;
            options.CallbackPath = MicrosoftSso.CallbackPath;
            options.GetClaimsFromUserInfoEndpoint = false;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");
            // The one that matters: without the API's own scope Entra issues a token for this site,
            // and the API is right to refuse it.
            options.Scope.Add(microsoftSso.ApiScope);
        });
}

// Secure by default: every endpoint requires an authenticated user unless it explicitly opts out
// with [AllowAnonymous]. Without this, a controller that simply forgets [Authorize] is wide open,
// which is how the admin, users, settings and audit-log pages were reachable anonymously. New
// controllers are now locked down until someone deliberately opens them.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Behind a TLS-terminating proxy, which is every PaaS, Render included. The request reaches Kestrel
// over plain HTTP. Without this the app believes it is being served insecurely, and
// UseHttpsRedirection below sends the browser to https, which the proxy forwards back as http,
// forever. It also keeps the auth cookie's SameAsRequest secure policy from downgrading.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // The platform's edge is the only path into the container and its proxy address is not known in
    // advance, so the headers are trusted whatever their source, the usual PaaS pattern.
    // KnownNetworks rather than KnownIPNetworks: this project targets net8.0, where the newer
    // property does not exist yet.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpContextAccessor();

// The institution's details are read by the footer on every page, so they are cached briefly
// rather than fetched per view.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IInstitutionDetails, InstitutionDetails>();

// Read by the sidebar on every page, so it caches its answer rather than asking each time.
builder.Services.AddScoped<ICommitteeEligibility, CommitteeEligibility>();
builder.Services.AddScoped<IDecisionComments, DecisionComments>();
builder.Services.AddScoped<IPipelineSteps, PipelineSteps>();

// ---------- Auth bridging services ----------
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddTransient<ApiAvailabilityHandler>();
builder.Services.AddScoped<ForceReauthFilter>();
builder.Services.AddScoped<ApiUnavailableFilter>();

// AuthApiClient deliberately carries no BearerTokenHandler: that handler depends on
// IAuthCookieService, which depends on AuthApiClient for token refresh, and attaching the handler
// here would be a DI cycle. Its one authenticated endpoint (change-password) takes the token as an
// explicit parameter instead.
builder.Services.AddHttpClient<AuthApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ApiOptions>>().Value.BaseUrl);
}).AddHttpMessageHandler<ApiAvailabilityHandler>();

// Everything else goes through BearerTokenHandler for automatic Bearer-attach + refresh-and-retry.
void ConfigureApiClient(IServiceProvider sp, HttpClient client) =>
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ApiOptions>>().Value.BaseUrl);

builder.Services.AddHttpClient<ContainersApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<ProposalsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<SupervisorGroupsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<EthicsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<PublicationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<DepartmentsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<UsersApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<CommitteesApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<AdminApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<SettingsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();

// The handler is needed for the administrator's calls, which the API restricts to Admin. It is
// harmless on the two anonymous ones. An invited person has no token, so nothing is attached and
// nothing is refreshed.
builder.Services.AddHttpClient<InvitationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();
builder.Services.AddHttpClient<NotificationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>().AddHttpMessageHandler<ApiAvailabilityHandler>();

// The published catalogue is anonymous end to end, so no bearer handler: a visitor who has never
// signed in has no token to attach, and requiring one would make the catalogue non-public.
builder.Services.AddHttpClient<CatalogueApiClient>(ConfigureApiClient).AddHttpMessageHandler<ApiAvailabilityHandler>();

// ---------- MVC ----------
// The sidebar records where somebody put a menu item with a fetch rather than a form, so the
// token has to be allowed to travel in a header. The default is form fields only.
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ForceReauthFilter>();

    // After ForceReauthFilter: an expired session is worth redirecting to sign-in even if the
    // call that discovered it also failed to reach the API.
    options.Filters.Add<ApiUnavailableFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development only: never reuse a cached *page*, so a change to a view or a controller is
    // visible on the next navigation.
    //
    // Deliberately not applied to css/js. It was, and it made every navigation re-download the
    // whole stylesheet, half a megabyte of Tabler, so the browser painted unstyled HTML and
    // everything jumped into place once it arrived. Assets are already versioned by asp-append-
    // version, whose query string changes the moment a file does, so caching them cannot serve
    // anything stale.
    app.Use(async (context, next) =>
    {
        // Set on response start so it wins over headers added later (e.g. by UseStaticFiles), and
        // so the content type is known, which is what tells a page from an asset.
        context.Response.OnStarting(() =>
        {
            var isDocument = context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
            if (!isDocument)
            {
                return Task.CompletedTask;
            }

            var headers = context.Response.Headers;
            headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            headers.Pragma = "no-cache";
            headers.Expires = "0";
            return Task.CompletedTask;
        });

        await next();
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// The whole product is British English, so pin the culture rather than inheriting whatever the
// host machine happens to be set to: it decides date and number formatting, and how posted
// dates are parsed. Views that need a fixed layout still say so explicitly.
var britishEnglish = new CultureInfo("en-GB");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(britishEnglish),
    SupportedCultures = [britishEnglish],
    SupportedUICultures = [britishEnglish]
});

// Before anything that inspects the scheme or the client address.
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Deliberately shallow: it answers whether this process is up and serving, not whether the API
// behind it is. A deployment gate that failed because the backend was briefly unavailable would
// roll back a frontend that is perfectly fine.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestampUtc = DateTime.UtcNow }))
    .AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
