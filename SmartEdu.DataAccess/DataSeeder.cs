using SmartEdu.DataAccess.EntityModels;
using SmartEdu.RazorWeb.Data;
using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            if (context.Users.Any(u => u.Role == UserRole.Admin))
            {
                return;
            }

            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "System Administrator",
                Email = "admin@smartedu.com",
                Role = UserRole.Admin,
                IsDeleted = false,
                RequirePasswordChange = false
            };

            context.Users.Add(admin);

            await context.SaveChangesAsync();
        }
    }
}
