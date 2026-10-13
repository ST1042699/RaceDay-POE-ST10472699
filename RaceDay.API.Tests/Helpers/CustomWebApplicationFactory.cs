using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Explicitly set the environment to "Testing" so Program.cs skips SQL Server
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Register the In-Memory database for tests
                services.AddDbContext<RaceDayDbContext>(options =>
                {
                    options.UseInMemoryDatabase("RaceDayTestDb");
                });

                // Build the service provider and seed data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

                db.Database.EnsureCreated();

                // Seed roles if they don't exist
                if (!db.Roles.Any())
                {
                    db.Roles.AddRange(
                        new Role { RoleID = 1, Rolename = "Organiser" },
                        new Role { RoleID = 2, Rolename = "Participant" }
                    );
                    db.SaveChanges();
                }
            });
        }
    }
}