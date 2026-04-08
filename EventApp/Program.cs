using EventApp.Data;
using EventApp.Models;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
var authCookieName = builder.Environment.IsDevelopment()
    ? "EventAndTicketAuth"
    : "__Host-EventAndTicketAuth";
var antiForgeryCookieName = builder.Environment.IsDevelopment()
    ? "EventAndTicketAntiForgery"
    : "__Host-EventAndTicketAntiForgery";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = authCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.IsEssential = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = cookieSecurePolicy;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = antiForgeryCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

var app = builder.Build();

app.CreateDbIfNotExists();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error");
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net data:; " +
        "script-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

public static class HostExtensions
{
    public static void CreateDbIfNotExists(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            context.Database.Migrate();
            SeedRoles(roleManager).Wait();
            SeedAdminUser(userManager, configuration).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred creating the DB or seeding roles: {ex.Message}");
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "Organizer", "Attendee" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(email);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Unable to seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addRoleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Unable to assign the Admin role: {errors}");
            }
        }
    }
}
