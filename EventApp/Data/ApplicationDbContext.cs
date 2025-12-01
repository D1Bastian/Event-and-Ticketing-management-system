using Microsoft.EntityFrameworkCore;
using EventApp.Models;
// 1. ADD: Required for IdentityDbContext
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; 

namespace EventApp.Data
{
    // 2. CHANGE: Inherit from IdentityDbContext<ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 3. IMPORTANT: Must be called FIRST when using IdentityDbContext 
            base.OnModelCreating(modelBuilder); 

            // Force UTC for all DateTime properties
            // Added a check to skip Identity tables
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Skip Identity tables to prevent issues with internal DateTimes
                if (entityType.GetTableName() != "AspNetUsers" && entityType.GetTableName() != "AspNetRoles" && entityType.GetTableName() != "AspNetUserRoles" )
                {
                    foreach (var property in entityType.GetProperties())
                    {
                        if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                        {
                            property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                        }
                    }
                }
            }
        }

        // Your existing DbSets remain for your core application data
        public DbSet<Event> Events { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
    }
}