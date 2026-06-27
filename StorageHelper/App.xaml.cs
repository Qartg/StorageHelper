using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Services.Automation;
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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .WriteTo.File("logs\\log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug()
                .CreateLogger();

            var cfg = new ConfigService("Config/Config.json");
            cfg.Load();

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddSerilog(dispose: true));
            //Config
            services.AddSingleton<IConfigService, ConfigService>(sp => cfg);
            services.AddSingleton(sp => sp.GetRequiredService<IConfigService>().Current);
            //Db
            services.AddDbContextFactory<StorageContext>((sp, opt) => opt.UseSqlite(cfg.Current.ConnectionString));
            //services
            services.AddSingleton<IDataBaseService, SqliteDataBase>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IPricingService, PricingService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IBrowserSession>(sp =>new BrowserSession(
                Path.Combine(AppContext.BaseDirectory, OzonConstants.BrowserProfileName),
                sp.GetRequiredService<ILogger<BrowserSession>>()));

            var type = cfg.Current.FakeAutomation ? typeof(FakeVendorAutomation) : typeof(OzonAutomation);
            services.AddSingleton(typeof(IVendorAutomation), type);
            //vm windows
            services.AddSingleton<StorageViewModel>();
            services.AddSingleton<MainWindow>();
            //factory
            services.AddSingleton<Func<Item, ItemCardViewModel>>(sp => item => new(sp.GetRequiredService<IDataBaseService>(),
                item, sp.GetRequiredService<IPricingService>(), sp.GetRequiredService<ILogger<ItemCardViewModel>>()));
            services.AddSingleton<Func<Item?, ItemEditViewModel>>(sp => item => new(sp.GetRequiredService<IDataBaseService>(), item, sp.GetRequiredService<IVendorAutomation>(), sp.GetRequiredService<IPricingService>()));
            services.AddSingleton<Func<AppSettings>>(sp => () => sp.GetRequiredService<IConfigService>().Current);
            services.AddSingleton<Func<LoginViewModel>>(sp => () => new(sp.GetRequiredService<IAuthService>()));
            services.AddSingleton<Func<IEnumerable<ReviewLine>, decimal?, ReviewViewModel>>((sp) => (lines, total) => new(sp.GetRequiredService<IPricingService>(), lines, total));
            services.AddSingleton<Func<IEnumerable<ReviewLine>, AutomationViewModel>>(sp =>
                new Func<IEnumerable<ReviewLine>, AutomationViewModel>(items =>
                    new AutomationViewModel(
                        sp.GetRequiredService<IVendorAutomation>(),
                        sp.GetRequiredService<IPricingService>(),
                        sp.GetRequiredService<ILogger<AutomationViewModel>>(),
                        items
                    )));
            //build
            ServiceProvider = services.BuildServiceProvider();

            using var db = ServiceProvider.GetRequiredService<IDbContextFactory<StorageContext>>().CreateDbContext();
            db.Database.Migrate();

            //TODO: убрать
            if (!db.Items.Any())
            {
                db.Items.AddRange(new List<Item>
                {
                    new Item()
                    {
                        Sku = "MED-STETH-01",
                        Name = "Стетоскоп Littmann Classic III",
                        Description = "Высокочувствительный акустический стетоскоп для аускультации взрослых и детей.",
                        Notes = "Требует бережного хранения, не допускать перегибов трубки.",
                        Vendor = "3M Littmann",
                        ImageURL = "https://example.com/images/steth01.jpg",
                        ParLevel = 10,
                        CurrentOnStorage = 12,
                        IsActive = true,
                        IsOrderable = false,
                        PriceRecords =
                        {
                            new PriceRecord { Price = 12500.00m, CapturedAt = new DateTime(2026, 01, 10) },
                            new PriceRecord { Price = 11900.00m, CapturedAt = new DateTime(2026, 02, 15) },
                            new PriceRecord { Price = 13200.00m, CapturedAt = new DateTime(2026, 04, 01) },
                            new PriceRecord { Price = 12800.00m, CapturedAt = new DateTime(2026, 06, 10) }
                        }
                    },
                    new Item()
                    {
                        Sku = "MED-BP-OMRON",
                        Name = "Тонометр автоматический Omron M3 Comfort",
                        Description = "Прибор для измерения артериального давления на плечо с умной манжетой Intelli Wrap.",
                        Notes = "Поставляется с адаптером сети и батарейками.",
                        Vendor = "Omron Healthcare",
                        ImageURL = "https://example.com/images/omronm3.jpg",
                        ParLevel = 15,
                        CurrentOnStorage = 8,
                        IsActive = true,
                        IsOrderable = true,
                        PriceRecords =
                        {
                            new PriceRecord { Price = 5400.00m, CapturedAt = new DateTime(2026, 01, 15) },
                            new PriceRecord { Price = 5800.00m, CapturedAt = new DateTime(2026, 03, 05) },
                            new PriceRecord { Price = 5100.00m, CapturedAt = new DateTime(2026, 04, 20) },
                            new PriceRecord { Price = 5650.00m, CapturedAt = new DateTime(2026, 06, 01) }
                        }
                    },
                    new Item()
                    {
                        Sku = "MED-PO-CHOICE",
                        Name = "Пульсоксиметр напалечный ChoiceMMed MD300C12",
                        Description = "Компактный прибор для контроля насыщения крови кислородом (SpO2) и частоты пульса.",
                        Notes = "Откалиброван, готов к эксплуатации.",
                        Vendor = "ChoiceMMed",
                        ImageURL = "https://example.com/images/pulseox.jpg",
                        ParLevel = 30,
                        CurrentOnStorage = 45,
                        IsActive = true,
                        IsOrderable = true,
                        PriceRecords =
                        {
                            new PriceRecord { Price = 1850.00m, CapturedAt = new DateTime(2026, 02, 01) },
                            new PriceRecord { Price = 1990.00m, CapturedAt = new DateTime(2026, 03, 12) },
                            new PriceRecord { Price = 1600.00m, CapturedAt = new DateTime(2026, 04, 18) },
                            new PriceRecord { Price = 1750.00m, CapturedAt = new DateTime(2026, 05, 25) },
                            new PriceRecord { Price = 1800.00m, CapturedAt = new DateTime(2026, 06, 14) }
                        }
                    },
                    new Item()
                    {
                        Sku = "MED-THERM-IR",
                        Name = "Термометр инфракрасный бесконтактный B.Well WF-4000",
                        Description = "Медицинский термометр для мгновенного измерения температуры тела, воды и воздуха.",
                        Notes = "Погрешность измерения составляет всего ±0.2 °C.",
                        Vendor = "B.Well",
                        ImageURL = "https://example.com/images/bwell4000.jpg",
                        ParLevel = 20,
                        CurrentOnStorage = 5,
                        IsActive = true,
                        IsOrderable = true,
                        PriceRecords =
                        {
                            new PriceRecord { Price = 3200.00m, CapturedAt = new DateTime(2026, 01, 20) },
                            new PriceRecord { Price = 3500.00m, CapturedAt = new DateTime(2026, 02, 28) },
                            new PriceRecord { Price = 2950.00m, CapturedAt = new DateTime(2026, 04, 10) },
                            new PriceRecord { Price = 3100.00m, CapturedAt = new DateTime(2026, 05, 30) }
                        }
                    },
                    new Item()
                    {
                        Sku = "MED-SCALP-15",
                        Name = "Скальпель хирургический одноразовый №15 (упаковка 10 шт)",
                        Description = "Стерильный одноразовый скальпель из нержавеющей стали с пластиковой ручкой.",
                        Notes = "Товар строго подлежит утилизации после вскрытия упаковки.",
                        Vendor = "Apexmed",
                        ImageURL = "https://example.com/images/scalpel15.jpg",
                        ParLevel = 50,
                        CurrentOnStorage = 120,
                        IsActive = true,
                        IsOrderable = true,
                        PriceRecords =
                        {
                            new PriceRecord { Price = 850.00m, CapturedAt = new DateTime(2026, 01, 05) },
                            new PriceRecord { Price = 920.00m, CapturedAt = new DateTime(2026, 02, 20) },
                            new PriceRecord { Price = 790.00m, CapturedAt = new DateTime(2026, 04, 05) },
                            new PriceRecord { Price = 880.00m, CapturedAt = new DateTime(2026, 05, 18) }
                        }
                    }
                });
                db.SaveChanges();
            }

            var window = ServiceProvider.GetRequiredService<MainWindow>();
            window.Show();
        }

    }

}
