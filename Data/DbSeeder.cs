using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using artapi.Models;

namespace artapi.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<AppDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<User>>();

            // Ensure database is created/migrated
            //await context.Database.MigrateAsync();

            // Seed roles
            string[] roles = ["User", "Artist", "Admin"];
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed admin user
            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new User { UserName = adminEmail, Email = adminEmail, Role = UserRole.Admin };
                var createResult = await userManager.CreateAsync(admin, "AdminPassword123!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Artists (and corresponding Identity users)
            var artistsData = new List<(string Name, string Email, string ImageUrl, string Bio)>
            {
                (
                    "Maelle Dessendre",
                    "maelleDes@abracadabra.com",
                    "https://images.steamusercontent.com/ugc/10549703576396892245/C9D46CDB01391AD4652F41B34904B3C75D084477/?imw=637&imh=358&ima=fit&impolicy=Letterbox&imcolor=%23000000&letterbox=true",
                    "Maelle is a fiercely talented artist and a masterful duelist,\r\nblending elegance and edge in everything she does. Her creations captivate,\r\nher blade commands respect—and whether on canvas or in combat, she never misses her mark."
                ),
                (
                    "Alex Shadow",
                    "AlexDarktherThanEdge@abracadabra.com",
                    "https://images.unsplash.com/photo-1599566150163-29194dcaad36?ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&q=80&w=687",
                    "Alex is a bold and talented painter whose edgy, distinctive style turns heads and challenges norms.\r\nKnown in the art world as ‘Alex the Edger,’ their work cuts through convention with raw expression and fearless creativity."
                )
            };

            foreach (var artistInfo in artistsData)
            {
                // Skip if already exists
                if (await context.Artists.AnyAsync(a => a.Email == artistInfo.Email))
                    continue;

                // Create backing Identity user if needed
                var existingUser = await userManager.FindByEmailAsync(artistInfo.Email);
                if (existingUser is null)
                {
                    var user = new User
                    {
                        UserName = artistInfo.Email,
                        Email = artistInfo.Email,
                        Role = UserRole.Artist
                    };

                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (!result.Succeeded)
                        continue; // skip creating the artist if the user couldn't be created

                    await userManager.AddToRoleAsync(user, "Artist");
                    existingUser = user;
                }

                // Create Artist entity
                var artist = new Artist
                {
                    Name = artistInfo.Name,
                    Email = artistInfo.Email,
                    Bio = artistInfo.Bio,
                    ImageUrl = artistInfo.ImageUrl,
                    UserId = existingUser.Id
                };

                context.Artists.Add(artist);
            }

            await context.SaveChangesAsync();

            // Seed Auctions/Paintings
            var maelle = await context.Artists.FirstOrDefaultAsync(a => a.Email == "maelleDes@abracadabra.com");
            var alex = await context.Artists.FirstOrDefaultAsync(a => a.Email == "AlexDarktherThanEdge@abracadabra.com");

            var auctionsData = new List<Auction>();
            if (maelle is not null)
            {
                auctionsData.Add(new Auction(
                    "Fencing Duel",
                    maelle.Id,
                    maelle.Name,
                    "https://images.unsplash.com/photo-1549289524-06cf8837ace5?w=800&q=80",
                    500,
                    "Oil on Canvas",
                    "24x36",
                    maelle.Bio
                ));
            }
            if (alex is not null)
            {
                auctionsData.Add(new Auction(
                    "Edge of Darkness",
                    alex.Id,
                    alex.Name,
                    "https://images.unsplash.com/photo-1583119912267-cc97c911e416?w=800&q=80",
                    750,
                    "Acrylic",
                    "30x40",
                    alex.Bio
                ));
            }

            foreach (var auction in auctionsData)
            {
                if (!await context.Auctions.AnyAsync(a => a.Title == auction.Title))
                {
                    context.Auctions.Add(auction);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
