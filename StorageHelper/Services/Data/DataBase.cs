using Microsoft.EntityFrameworkCore;
using StorageHelper.Models;

namespace StorageHelper.Services
{
    public interface IDataBaseService
    {
        public Task<IEnumerable<Item>> GetItemsList();
        public Task AddItem(Item item);
        public Task<bool> UpdateItem(Item item);
        public Task<bool> SetIsActive(int itemId, bool active);
        public Task<bool> DeleteItem(int itemId);
    }

    public class SqliteDataBase : IDataBaseService
    {
        private readonly IDbContextFactory<StorageContext> _dbContext;

        public SqliteDataBase(IDbContextFactory<StorageContext> dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddItem(Item item)
        {
            using var db = await _dbContext.CreateDbContextAsync();

            db.Items.Add(item);
            await db.SaveChangesAsync();
        }

        public async Task<bool> DeleteItem(int itemId)
        {
            using var db = await _dbContext.CreateDbContextAsync();

            var ctxItem = await db.Items.FindAsync(itemId);
            if (ctxItem != null)
            {
                db.Items.Remove(ctxItem);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateItem(Item item)
        {
            using var db = await _dbContext.CreateDbContextAsync();

            var ctxItem = await db.Items.FindAsync(item.Id);
            if(ctxItem != null)
            {
                ctxItem.ImageURL = item.ImageURL;
                ctxItem.Name = item.Name;
                ctxItem.Description = item.Description; 
                ctxItem.CurrentOnStorage = item.CurrentOnStorage;
                ctxItem.ParLevel = item.ParLevel;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Item>> GetItemsList()
        {
            using var db = await _dbContext.CreateDbContextAsync();

            return await db.Items.Where((item) => item.IsActive || item.CurrentOnStorage > 0).ToListAsync();
        }

        public async Task<bool> SetIsActive(int itemId, bool active)
        {
            using var db = await _dbContext.CreateDbContextAsync();

            var ctxItem = await db.Items.FindAsync(itemId);
            if (ctxItem != null)
            {
                ctxItem.IsActive = active;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
