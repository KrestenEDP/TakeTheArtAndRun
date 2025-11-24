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
                    "Elena Rivera",
                    "elena.rivera@artgallery.com",
                    "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=400&h=400&fit=crop&crop=face",
                    "Elena is a contemporary abstract painter known for her vibrant use of color and geometric forms. Her work explores themes of urban landscapes and human emotion through bold, expressive brushstrokes."
                ),
                (
                    "Marcus Chen",
                    "marcus.chen@artgallery.com", 
                    "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=400&h=400&fit=crop&crop=face",
                    "Marcus specializes in mixed media sculptures that blend traditional craftsmanship with modern materials. His pieces often incorporate found objects to create thought-provoking commentaries on consumer culture."
                ),
                (
                    "Sophie Laurent",
                    "sophie.laurent@artgallery.com",
                    "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&h=400&fit=crop&crop=face", 
                    "Sophie is a digital artist and photographer who creates surreal landscapes by combining traditional photography with digital manipulation. Her work blurs the line between reality and imagination."
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
            var elena = await context.Artists.FirstOrDefaultAsync(a => a.Email == "elena.rivera@artgallery.com");
            var marcus = await context.Artists.FirstOrDefaultAsync(a => a.Email == "marcus.chen@artgallery.com");
            var sophie = await context.Artists.FirstOrDefaultAsync(a => a.Email == "sophie.laurent@artgallery.com");

            var elenaDescription = "Elena Rivera is a contemporary abstract painter known for her vibrant use of color and geometric forms. Her work explores themes of urban landscapes and human emotion through bold, expressive brushstrokes.";
            var marcusDescription = "Marcus Chen specializes in mixed media sculptures that blend traditional craftsmanship with modern materials. His pieces often incorporate found objects to create thought-provoking commentaries on consumer culture.";
            var sophieDescription = "Sophie Laurent is a digital artist and photographer who creates surreal landscapes by combining traditional photography with digital manipulation. Her work blurs the line between reality and imagination.";

            var auctionsData = new List<Auction>();

            // Elena's artworks (5 pieces) - Abstract paintings
            if (elena is not null)
            {
                auctionsData.Add(new Auction("Urban Symphony", elena.Id, elena.Name, "https://images.unsplash.com/photo-1579783928621-7a13d66a62d1", 850, elenaDescription));
                auctionsData.Add(new Auction("Geometric Dreams", elena.Id, elena.Name, "https://images.unsplash.com/photo-1578301978693-85fa9c0320b9", 650, elenaDescription));
                auctionsData.Add(new Auction("Color Burst", elena.Id, elena.Name, "https://images.unsplash.com/flagged/photo-1572392640988-ba48d1a74457", 720, elenaDescription));
                auctionsData.Add(new Auction("Metropolitan Rhythm", elena.Id, elena.Name, "https://images.unsplash.com/photo-1578926375605-eaf7559b1458", 900, elenaDescription));
                auctionsData.Add(new Auction("Harmony in Chaos", elena.Id, elena.Name, "https://images.unsplash.com/photo-1579965342575-16428a7c8881", 780, elenaDescription));
            }

            if (marcus is not null)
            {
                auctionsData.Add(new Auction("Reclaimed Beauty", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1515405295579", 1200, marcusDescription));
                auctionsData.Add(new Auction("Industrial Harmony", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1549289524-06cf8837ace5", 950, marcusDescription));
                auctionsData.Add(new Auction("Memory Fragments", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262", 800, marcusDescription));
                auctionsData.Add(new Auction("Urban Totem", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1579783901586-d88db74b4fe4", 1500, marcusDescription));
                auctionsData.Add(new Auction("Consumer Echo", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1578301978018-3005759f48f7", 675, marcusDescription));
                auctionsData.Add(new Auction("Metamorphosis", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1583119912267-cc97c911e416", 1100, marcusDescription));
            }

            if (sophie is not null)
            {
                auctionsData.Add(new Auction("Digital Landscape #1", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1579541814924-49fef17c5be5", 450, sophieDescription));
                auctionsData.Add(new Auction("Neon Dreams", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1578301996581-bf7caec556c0", 380, sophieDescription));
                auctionsData.Add(new Auction("Ocean of Stars", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1582561424760-0321d75e81fa", 520, sophieDescription));
                auctionsData.Add(new Auction("Virtual Reality", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119", 600, sophieDescription));
                auctionsData.Add(new Auction("Fractured Time", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1577083639236-0f560d3d771c", 480, sophieDescription));
                auctionsData.Add(new Auction("Electric Forest", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1584446922442-7ac6b8c118f3", 420, sophieDescription));
                auctionsData.Add(new Auction("Quantum Garden", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1599894019794-50339c9ad89c", 550, sophieDescription));
                auctionsData.Add(new Auction("Mirror Dimensions", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1615184697985-c9bde1b07da7", 625, sophieDescription));
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
