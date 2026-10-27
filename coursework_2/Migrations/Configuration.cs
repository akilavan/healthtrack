namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Web.Helpers;

    internal sealed class Configuration : DbMigrationsConfiguration<coursework_2.Models.HealthTrackContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(coursework_2.Models.HealthTrackContext context)
        {
            context.Roles.AddOrUpdate(r => r.RoleName,
                new coursework_2.Models.Role { RoleName = "Admin" },
                new coursework_2.Models.Role { RoleName = "Trainer" },
                new coursework_2.Models.Role { RoleName = "Client" });
            context.SaveChanges();

            var adminRole = context.Roles.Single(r => r.RoleName == "Admin");
            var trainerRole = context.Roles.Single(r => r.RoleName == "Trainer");
            var clientRole = context.Roles.Single(r => r.RoleName == "Client");

            if (!context.Users.Any(u => u.Email == "admin@healthtrack.lk"))
            {
                context.Users.Add(new coursework_2.Models.User
                {
                    RoleId = adminRole.RoleId,
                    Email = "admin@healthtrack.lk",
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword("Admin@123"),
                    FullName = "System Admin",
                    Gender = "Male",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!context.Users.Any(u => u.Email == "trainer@healthtrack.lk"))
            {
                context.Users.Add(new coursework_2.Models.User
                {
                    RoleId = trainerRole.RoleId,
                    Email = "trainer@healthtrack.lk",
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword("Trainer@123"),
                    FullName = "Nimal Perera",
                    Gender = "Male",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!context.Users.Any(u => u.Email == "client@healthtrack.lk"))
            {
                context.Users.Add(new coursework_2.Models.User
                {
                    RoleId = clientRole.RoleId,
                    Email = "client@healthtrack.lk",
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword("Client@123"),
                    FullName = "Kamal Silva",
                    Gender = "Male",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.SaveChanges();
        }
    }
}
