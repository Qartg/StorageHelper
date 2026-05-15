using Microsoft.Data.Sqlite;
using StorageHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace StorageHelper.Services
{
    public interface IDataBaseService
    {
        public Task<IEnumerable<Item>> GetItemsList();
        public Task UpdateItemsList(IEnumerable<Item> newItems);
    }

    public class SqliteDataBase : IDataBaseService
    {
        private readonly string _connectionString;

        public SqliteDataBase(string connectionString)
        {
            _connectionString = connectionString;

            InitializeDataBase();
        }

        private void InitializeDataBase()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"CREATE TABLE IF NOT EXISTS StorageItems (
                               Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                Count INT CHECK (Count >= 0));";

                using var command = new SqliteCommand(sql, conn);
                command.ExecuteNonQuery();
            }
        }

        public async Task<IEnumerable<Item>> GetItemsList()
        {
            List<Item> loadedItems = new();

            using (var conn = new SqliteConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = "SELECT Id, Name, Count FROM StorageItems;";

                using (var command = new SqliteCommand(sql, conn))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Item item = new Item()
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CurrentOnStorage = reader.GetInt32(2)
                        };

                        loadedItems.Add(item);
                    }
                }
            }

            return loadedItems;
        }

        public async Task UpdateItemsList(IEnumerable<Item> newItems)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var transaction = await conn.BeginTransactionAsync())
                {
                    string sql = @"INSERT INTO StorageItems (Id, Name, Count)
                                   VALUES ($id, $name, $count)
                                   ON CONFLICT(Id) DO UPDATE SET Count = $count, Name = $name;";

                    using (var command = new SqliteCommand(sql, conn))
                    {
                        var idParam = command.Parameters.Add("$id", SqliteType.Integer);
                        var nameParam = command.Parameters.Add("$name", SqliteType.Text);
                        var countParam = command.Parameters.Add("$count", SqliteType.Integer);

                        foreach (var item in newItems)
                        {
                            idParam.Value = item.Id == 0 ? DBNull.Value : item.Id;
                            nameParam.Value = item.Name;
                            countParam.Value = item.CurrentOnStorage;

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                }
            }
        }
    }
}
