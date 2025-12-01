using EventApp.Data;
using EventApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System;

// ---------------------
// SERILOG CONFIGURATION
// ---------------------
var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Configure Serilog BEFORE services
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // reads from appsettings.json
    .Enrich.FromLogContext()
    .CreateLogger();

// Tell the host to use Serilog
builder.Host.UseSerilog();

// ---------------------
// SERVICES CONFIGURATION
// ---------------------

// Identity requires Razor Pages support
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// PostgreSQL connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity configuration with Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Optional password rules
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>() // Add Role support
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// ---------------------
// DATABASE AND ROLE SEEDING
// ---------------------
app.CreateDbIfNotExists(); // custom extension method to seed roles

// ---------------------
// MIDDLEWARE
// ---------------------

// Custom 404/500 error handling
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Serilog request logging middleware (must be AFTER app is built)
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

// Controller & Razor Pages endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// ---------------------
// ROLE SEEDING EXTENSION METHOD
// ---------------------
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

            // Apply migrations
            context.Database.Migrate();

            // Seed roles
            SeedRoles(roleManager).Wait();
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
}
