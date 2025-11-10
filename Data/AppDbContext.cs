using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TakeTheArtAndRunAPI.Models;

namespace TakeTheArtAndRunAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Auction)
            .WithMany()
            .HasForeignKey(t => t.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
