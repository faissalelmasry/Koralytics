using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Seeding
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // Seed Roles
            var roles = new[] { "Scouter", "SystemAdmin", "AcademyAdmin", "Player", "Parent", "Coach" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role { Name = roleName });
                }
            }

            // Seed DrillCategories
            var categories = new[] { "Passing", "Shooting", "Dribbling", "Defending", "GoalKeeping", "Speed", "Physical" };
            foreach (var categoryName in categories)
            {
                if (!await context.DrillCategories.AnyAsync(c => c.Name == categoryName))
                {
                    context.DrillCategories.Add(new DrillCategory { Name = categoryName });
                }
            }
            await context.SaveChangesAsync();

            // Seed System Admin account
            const string adminEmail = "admin@koralytics.com";
            const string adminUserName = "SystemAdmin";
            const string adminPassword = "Admin@123456";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new SystemAdminUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = 1
                };

                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [AspNetUsers] NOCHECK CONSTRAINT [FK_AspNetUsers_AspNetUsers_CreatedById]");

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    adminUser.CreatedById = adminUser.Id;
                    await context.SaveChangesAsync();
                    await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
                }

                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [AspNetUsers] CHECK CONSTRAINT [FK_AspNetUsers_AspNetUsers_CreatedById]");
            }
        }
    }
}
