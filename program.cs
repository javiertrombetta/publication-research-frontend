
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
//Note that now it's Controller with Views, not pure Controller

var app = builder.Build();


app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();