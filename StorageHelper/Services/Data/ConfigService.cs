using StorageHelper.Models;
using System.IO;
using System.Text.Json;

namespace StorageHelper.Services.Data
{
    public interface IConfigService
    {
        AppSettings Current { get; }
        Task LoadAsync();
        void Load();
        Task SaveAsync();
        void Save();
    }

    internal class ConfigService : IConfigService
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _json;
        public AppSettings Current { get; private set; } = new();

        public ConfigService(string filePath, JsonSerializerOptions? json = null)
        {
            _filePath = filePath;
            _json = json ?? new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true
            };
        }

        public async Task LoadAsync()
        {
            if (File.Exists(_filePath))
            {
                using FileStream fs = File.OpenRead(_filePath);
                try
                {
                    var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, _json).ConfigureAwait(false);
                    if (loaded is not null)
                        Current = loaded;
                }
                catch (Exception)
                {
                    Current = new AppSettings();
                    await SaveAsync().ConfigureAwait(false);
                }
            }
            else
            {
                Current = new AppSettings();
                await SaveAsync().ConfigureAwait(false);
            }
        }

        public async Task SaveAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            using var fs = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(fs, Current, _json);
        }

        public void Load()
        {
            if (File.Exists(_filePath))
            {
                using FileStream fs = File.OpenRead(_filePath);
                try
                {
                    var loaded = JsonSerializer.Deserialize<AppSettings>(fs, _json);
                    if (loaded is not null)
                        Current = loaded;
                }
                catch (Exception)
                {
                    Current = new AppSettings();
                    Save();
                }
            }
            else
            {
                Current = new AppSettings();
                Save();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            using var fs = File.Create(_filePath);
            JsonSerializer.Serialize(fs, Current, _json);
        }
    }
}
