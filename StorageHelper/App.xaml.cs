using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.ViewModels;
using System.IO;
using System.Windows;

namespace StorageHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IServiceProvider ServiceProvider { get; set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string connString = config.GetConnectionString("DefaultConnection") ?? "Data Source=Storage.db";

            var services = new ServiceCollection();
            //Db
            services.AddDbContextFactory<StorageContext>(opt => opt.UseSqlite(connString));
            services.AddSingleton<IDataBaseService, SqliteDataBase>();
            //vm windows
            services.AddSingleton<StorageViewModel>();
            services.AddSingleton<MainWindow>();
            //factory
            services.AddSingleton<Func<Item, ItemCardViewModel>>(sp => item => new(sp.GetRequiredService<IDataBaseService>(), item));

            //build
            ServiceProvider = services.BuildServiceProvider();

            MigrageDataBase();

            var window = ServiceProvider.GetRequiredService<MainWindow>();
            window.Show();
        }

        private void MigrageDataBase()
        {
            using var db = ServiceProvider.GetRequiredService<IDbContextFactory<StorageContext>>().CreateDbContext();
            db.Database.Migrate();
        }

    }

}
