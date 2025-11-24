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
                    "https://images.unsplash.com/photo-1494790108755-2616b612b77c?w=400&h=400&fit=crop&crop=face",
                    "Elena is a contemporary abstract painter known for her vibrant use of color and geometric forms. Her work explores themes of urban landscapes and human emotion through bold, expressive brushstrokes."
                ),
                (
                    "Marcus Chen",
                    "marcus.chen@artgallery.com", 
                    "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop&crop=face",
                    "Marcus specializes in mixed media sculptures that blend traditional craftsmanship with modern materials. His pieces often incorporate found objects to create thought-provoking commentaries on consumer culture."
                ),
                (
                    "Sophie Laurent",
                    "sophie.laurent@artgallery.com",
                    "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400&h=400&fit=crop&crop=face", 
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

            var auctionsData = new List<Auction>();

            // Elena's artworks (5 pieces)
            if (elena is not null)
            {
                auctionsData.Add(new Auction("Urban Symphony", elena.Id, elena.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&q=80", 850, "Acrylic on Canvas", "36x48", "A vibrant abstract piece capturing the energy of city life"));
                auctionsData.Add(new Auction("Geometric Dreams", elena.Id, elena.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 650, "Mixed Media", "24x36", "Bold geometric forms in warm earth tones"));
                auctionsData.Add(new Auction("Color Burst", elena.Id, elena.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 720, "Oil on Canvas", "30x40", "An explosion of primary colors in abstract form"));
                auctionsData.Add(new Auction("Metropolitan Rhythm", elena.Id, elena.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&q=80", 900, "Acrylic on Canvas", "48x60", "Large scale abstract representing urban movement"));
                auctionsData.Add(new Auction("Harmony in Chaos", elena.Id, elena.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 780, "Mixed Media", "32x42", "Finding balance in abstract composition"));
            }

            // Marcus's sculptures (6 pieces)
            if (marcus is not null)
            {
                auctionsData.Add(new Auction("Reclaimed Beauty", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 1200, "Mixed Media Sculpture", "24x18x12", "Sculpture made from recycled materials"));
                auctionsData.Add(new Auction("Industrial Harmony", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&q=80", 950, "Steel and Wood", "36x24x18", "Fusion of industrial and organic materials"));
                auctionsData.Add(new Auction("Memory Fragments", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 800, "Found Objects", "18x12x8", "Assemblage of personal artifacts"));
                auctionsData.Add(new Auction("Urban Totem", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&q=80", 1500, "Mixed Media", "72x12x12", "Tall sculptural piece reflecting city life"));
                auctionsData.Add(new Auction("Consumer Echo", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&q=80", 675, "Plastic and Metal", "20x15x10", "Commentary on consumer culture"));
                auctionsData.Add(new Auction("Metamorphosis", marcus.Id, marcus.Name, "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&q=80", 1100, "Bronze and Glass", "30x20x15", "Transformation through materials"));
            }

            // Sophie's digital art (8 pieces)
            if (sophie is not null)
            {
                auctionsData.Add(new Auction("Digital Landscape #1", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&q=80", 450, "Digital Print", "24x36", "Surreal mountain landscape with digital effects"));
                auctionsData.Add(new Auction("Neon Dreams", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=800&q=80", 380, "Digital Print", "20x30", "Cyberpunk-inspired cityscape"));
                auctionsData.Add(new Auction("Ocean of Stars", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&q=80", 520, "Digital Print", "30x45", "Cosmic seascape blending reality and fantasy"));
                auctionsData.Add(new Auction("Virtual Reality", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=800&q=80", 600, "Digital Print", "36x48", "Exploration of digital vs physical reality"));
                auctionsData.Add(new Auction("Fractured Time", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&q=80", 480, "Digital Print", "28x40", "Time-lapse effects on natural landscape"));
                auctionsData.Add(new Auction("Electric Forest", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=800&q=80", 420, "Digital Print", "22x32", "Nature enhanced with digital lighting"));
                auctionsData.Add(new Auction("Quantum Garden", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&q=80", 550, "Digital Print", "32x44", "Botanical forms in impossible colors"));
                auctionsData.Add(new Auction("Mirror Dimensions", sophie.Id, sophie.Name, "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=800&q=80", 625, "Digital Print", "40x50", "Exploring parallel realities through visual manipulation"));
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
