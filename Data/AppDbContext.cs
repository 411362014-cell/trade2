using CCUTrade.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CCUTrade.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<Comment> Comments => Set<Comment>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       
        optionsBuilder.UseSqlServer(@"Server=LAPTOP-AIOUE31H\SQLEXPRESS;Database=CCUTrade;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}