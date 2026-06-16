using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StorageHelper.Models;

namespace StorageHelper.Services.Data
{
    public class StorageContextFactory : IDesignTimeDbContextFactory<StorageContext>
    {
        public StorageContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<StorageContext>()
                .UseSqlite("Data Source=Storage.db")
                .Options;
            return new StorageContext(options);
        }
    }
}
