using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Services.Data;
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

            var services = new ServiceCollection();

            //Config
            services.AddSingleton<IConfigService, ConfigService>(sp =>{
                var cfg = new ConfigService("Config/Config.json");
                cfg.Load();
                return cfg;
            });
            //Db
            services.AddDbContextFactory<StorageContext>((sp, opt) => 
            opt.UseSqlite(sp.GetRequiredService<IConfigService>().Current.ConnectionString)
            );
            //services
            services.AddSingleton<IDataBaseService, SqliteDataBase>();
            services.AddSingleton<IAuthService, AuthService>();
            //vm windows
            services.AddSingleton<StorageViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IDialogService, DialogService>();
            //factory
            services.AddSingleton<Func<Item, ItemCardViewModel>>(sp => item => new(sp.GetRequiredService<IDataBaseService>(), item));
            services.AddSingleton<Func<Item?, ItemEditViewModel>>(sp => item => new(sp.GetRequiredService<IDataBaseService>(), item));
            services.AddSingleton<Func<AppSettings>>(sp => () => sp.GetRequiredService<IConfigService>().Current);
            services.AddSingleton<Func<LoginViewModel>>(sp => () => new(sp.GetRequiredService<IAuthService>()));

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
