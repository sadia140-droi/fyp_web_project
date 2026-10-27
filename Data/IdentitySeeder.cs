using Cloud_Kitchen_web_App.Models;
using Microsoft.AspNetCore.Identity;

namespace CloudKitchen.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // ============================================================
            // CREATE ROLES IF NOT EXIST
            // ============================================================

            string[] roles =
            {
                "Customer",
                "Seller",
                "Admin"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }
        }

        public static Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
            => SeedRolesAsync(serviceProvider);
    }
}