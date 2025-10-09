using System.Threading.Tasks;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Backend.DbContext;
namespace Backend.Services;

public static class DbSeeder
{
    public static async Task Seed(
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // ResourceTypes
        if (!context.ResourceTypes.Any())
        {
            context.ResourceTypes.AddRange(
                new ResourceType { ResourceTypeName = "Drop-in skrivbord" },
                new ResourceType { ResourceTypeName = "Mötesrum" },
                new ResourceType { ResourceTypeName = "VR headset" },
                new ResourceType { ResourceTypeName = "AI server" }
            );
            await context.SaveChangesAsync();
        }

        // Resources
        if (!context.Resources.Any())
        {
            var dropinDesks = Enumerable.Range(1, 15)
                .Select(i => new Resource { ResourceTypeId = 1, ResourceName = $"Drop-in skrivbord {i}" });
            var meetingRooms = Enumerable.Range(1, 4)
                .Select(i => new Resource { ResourceTypeId = 2, ResourceName = $"Mötesrum {i}" });
            var vrHeadsets = Enumerable.Range(1, 4)
                .Select(i => new Resource { ResourceTypeId = 3, ResourceName = $"VR Headset {i}" });
            var aiServers = new[] { new Resource { ResourceTypeId = 4, ResourceName = "AI Server" } };

            context.Resources.AddRange(dropinDesks);
            context.Resources.AddRange(meetingRooms);
            context.Resources.AddRange(vrHeadsets);
            context.Resources.AddRange(aiServers);

            await context.SaveChangesAsync();
        }

        // Ensure admin role exists
        if (!await roleManager.RoleExistsAsync("admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("admin"));
        }

        // Ensure admin user exists
        var systemUser = await userManager.FindByNameAsync("admin");
        if (systemUser == null)
        {
            systemUser = new User
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@example.com",
                NormalizedEmail = "ADMIN@EXAMPLE.COM",
                Name = "System Admin"
            };

            var result = await userManager.CreateAsync(systemUser, "Admin@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(systemUser, "admin");
                Console.WriteLine("System user created and assigned to admin role.");
            }
            else
            {
                Console.WriteLine("Failed to create system user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
    }
}
