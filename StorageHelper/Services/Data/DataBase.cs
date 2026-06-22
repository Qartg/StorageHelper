using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StorageHelper.Models;

namespace StorageHelper.Services
{
    public interface IDataBaseService
    {
        public Task<IEnumerable<Item>> GetItemsList();
        public Task<bool> AddItem(Item item);
        public Task<bool> UpdateItem(Item item);
        public Task<bool> SetIsActive(int itemId, bool active);
        public Task<bool> DeleteItem(int itemId);
    }

    public class SqliteDataBase : IDataBaseService
    {
        private readonly IDbContextFactory<StorageContext> _dbContext;
        private readonly ILogger<SqliteDataBase> _logger;

        public SqliteDataBase(IDbContextFactory<StorageContext> dbContext, ILogger<SqliteDataBase> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> AddItem(Item item)
        {
            try
            {
                using var db = await _dbContext.CreateDbContextAsync();

                db.Items.Add(item);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddItem - {Sku}", item.Sku);
                return false;
            }
        }

        public async Task<bool> DeleteItem(int itemId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteItem id is - {itemId}", itemId);
                return false;
            }
        }

        public async Task<bool> UpdateItem(Item item)
        {
            try
            {
                using var db = await _dbContext.CreateDbContextAsync();

                var ctxItem = await db.Items.FindAsync(item.Id);
                if (ctxItem != null)
                {
                    ctxItem.Sku = item.Sku;
                    ctxItem.Name = item.Name;
                    ctxItem.Description = item.Description;
                    ctxItem.Notes = item.Notes;
                    ctxItem.Vendor = item.Vendor;
                    ctxItem.ImageURL = item.ImageURL;
                    ctxItem.ParLevel = item.ParLevel;
                    ctxItem.CurrentOnStorage = item.CurrentOnStorage;
                    ctxItem.IsActive = item.IsActive;
                    ctxItem.IsOredrable = item.IsOredrable;
                    await db.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateItem - {Sku}", item.Sku);
                return false;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsList()
        {
            try
            {
                using var db = await _dbContext.CreateDbContextAsync();
                return await db.Items.Include(i => i.PriceRecords).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetItemsList");
                return Enumerable.Empty<Item>();
            }
        }

        public async Task<bool> SetIsActive(int itemId, bool active)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSetIsActiveItem id is - {itemId}", itemId);
                return false;
            }
        }
    }
}
