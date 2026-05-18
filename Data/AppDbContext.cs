using CCUTrade.Models;
using Microsoft.EntityFrameworkCore;

namespace CCUTrade.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=ccutrade.db");
    }
}