using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Http;
using ResearchPublicationManagementSystem.Infrastructure.Options;
using ResearchPublicationManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options ----------
builder.Services.AddOptions<ApiOptions>().Bind(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.AddOptions<InstitutionOptions>().Bind(builder.Configuration.GetSection(InstitutionOptions.SectionName));

// ---------- Cookie authentication (holds the backend JWT access/refresh tokens as claims) ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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

// Secure by default: every endpoint requires an authenticated user unless it explicitly opts
// out with [AllowAnonymous]. Without this, a controller that simply forgets [Authorize] is
// wide open — which is how the admin, users, settings and audit-log pages were reachable
// anonymously. New controllers are now locked down until someone deliberately opens them.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();

// The institution's details are read by the footer on every page, so they are cached briefly
// rather than fetched per view.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IInstitutionDetails, InstitutionDetails>();

// ---------- Auth bridging services ----------
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddScoped<ForceReauthFilter>();

// AuthApiClient deliberately carries no BearerTokenHandler: that handler depends on
// IAuthCookieService, which depends on AuthApiClient for token refresh — attaching the handler
// here would be a DI cycle. Its one authenticated endpoint (change-password) takes the token
// as an explicit parameter instead.
builder.Services.AddHttpClient<AuthApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ApiOptions>>().Value.BaseUrl);
});

// Everything else goes through BearerTokenHandler for automatic Bearer-attach + refresh-and-retry.
void ConfigureApiClient(IServiceProvider sp, HttpClient client) =>
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ApiOptions>>().Value.BaseUrl);

builder.Services.AddHttpClient<ContainersApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<ProposalsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<EthicsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<PublicationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<DepartmentsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<UsersApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<CommitteesApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<AdminApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<SettingsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();

// The handler is needed for the administrator's calls, which the API restricts to Admin. It is
// harmless on the two anonymous ones — an invited person has no token, so nothing is attached and
// nothing is refreshed.
builder.Services.AddHttpClient<InvitationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<NotificationsApiClient>(ConfigureApiClient).AddHttpMessageHandler<BearerTokenHandler>();

// The published catalogue is anonymous end to end, so no bearer handler: a visitor who has never
// signed in has no token to attach, and requiring one would make the catalogue non-public.
builder.Services.AddHttpClient<CatalogueApiClient>(ConfigureApiClient);

// ---------- MVC ----------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ForceReauthFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development only: never reuse a cached *page*, so a change to a view or a controller is
    // visible on the next navigation.
    //
    // Deliberately not applied to css/js. It was, and it made every navigation re-download the
    // whole stylesheet — half a megabyte of Tabler — so the browser painted unstyled HTML and
    // everything jumped into place once it arrived. Assets are already versioned by
    // asp-append-version, whose query string changes the moment a file does, so caching them
    // cannot serve anything stale.
    app.Use(async (context, next) =>
    {
        // Set on response start so it wins over headers added later (e.g. by UseStaticFiles),
        // and so the content type is known — which is what tells a page from an asset.
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
