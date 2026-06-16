using Microsoft.EntityFrameworkCore;
using StorageHelper.Models;

namespace StorageHelper.Services
{
    public class StorageContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<PriceRecord> PriceRecords { get; set; }

        public StorageContext(DbContextOptions<StorageContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Item>().HasIndex(i => i.Sku).IsUnique();
    }
}
