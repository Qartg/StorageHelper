using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Win32;
using StorageHelper.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace StorageHelper.Services.Automation
{
    public class OzonAutomation : IVendorAutomation, IAsyncDisposable
    {
        private readonly ILogger<OzonAutomation> _logger;
        private IPlaywright? _pw;
        private IBrowser? _browser;
        private IPage? _page;
        private bool _ready;

        public IPage Page => _page ?? throw new InvalidOperationException("Страница не загружена");

        private const string OZON_BASE_LINK = "https://www.ozon.ru/";

        public OzonAutomation(ILogger<OzonAutomation> logger)
        {
            _logger = logger;
        }

        public async Task<AddToCartResult> AddItemToCart(string sku, int quantity, CancellationToken ct = default)
        {
            await Task.Delay(10000);
            return new(sku, false, null, null, "tset");
        }

        public async Task<GetItemInfoResult> GetItemInfo(string sku, CancellationToken ct = default)
        {
            try
            {
                if(!await EnsureStartedAsync(ct))
                    return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при открытии браузера");

                await OpenAsync($"{OZON_BASE_LINK}product/{sku}/?oos_search=false");

                var ld = await Page.Locator("script[type='application/ld+json']").First.TextContentAsync();
                if(ld == null)
                {
                    _logger.LogError("ld оказался пустым");
                    return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при получении информации о товаре");
                }

                using var doc = JsonDocument.Parse(ld);
                var root = doc.RootElement;

                if (!root.TryGetProperty("sku", out var v) || v.GetString() != sku)
                    return new(sku, false, null, null, null, null, null, null, "SKU не совпадают");

                var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
                var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
                var price = ParsePrice(in root);
                var image = root.TryGetProperty("image", out var i) ? i.GetString() : null;

                var vendorLocator = Page.Locator("a[href*='/seller/'][title]").First;
                var vendor = await vendorLocator.CountAsync() > 0 ? await vendorLocator.GetAttributeAsync("title") : null;

                bool outOfStock = await Page.Locator("[data-widget='webOutOfStock']").First.CountAsync() > 0;

                string msg = outOfStock ? "Товара нет в наличии" : "Успешно получил информацию о товаре";
                return new(sku, true, name, desc, vendor, image, !outOfStock, price, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить информацию о предмете: {sku}", sku);
            }

            return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при получении информации о товаре");
        }


        //TODO: port using 0 in argument
        private async Task<bool> EnsureStartedAsync(CancellationToken ct = default)
        {
            try
            {
                if (_ready && _page is not null) return true;
                _pw ??= await Playwright.CreateAsync();

                if(!await WaitForCDPAsync(9222, ct, 5))
                    TryStartBrowser();

                if (await WaitForCDPAsync(9222, ct))
                {
                    _browser ??= await _pw.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");
                    var ctx = _browser.Contexts.Count > 0 ? _browser.Contexts[0] : await _browser.NewContextAsync();
                    _page = ctx.Pages.Count > 0 ? ctx.Pages[0] : await ctx.NewPageAsync();
                    await EnsureLoggedInAsync(ct);
                    _ready = true;

                    return true;
                }
                else
                    _logger.LogError("CDP не поднялся в течении 15 секунд");

                _ready = false;
                return false;
            }
            catch (Exception ex)
            {
                _ready = false;
                _logger.LogError(ex, "Не удалось запустить сессию автоматизации");
                return false;
            }
        }

        private async Task EnsureLoggedInAsync(CancellationToken ct = default)
        {
            await OpenAsync(OZON_BASE_LINK);

            var loginBtn = Page.GetByText("Войти", new() { Exact = true }).First;

            if (await loginBtn.IsVisibleAsync())
            {
                await loginBtn.ClickAsync();
                //TODO: state or noty
                var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                
                while (deadline > DateTime.UtcNow)
                {
                    if (!await loginBtn.IsVisibleAsync())
                        return;

                    await Task.Delay(1000, ct);
                }

                throw new Exception("Превышено время ожидания при авторизации в аккаунт");
            }
        }

        private static async Task<bool> WaitForCDPAsync(int port, CancellationToken ct = default, double waitTime = 15)
        {
            using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(waitTime);

            while(DateTime.UtcNow < deadline)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var response = await http.GetAsync($"http://127.0.0.1:{port}/json/version", ct);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
                {
                    if(ct.IsCancellationRequested) return false;
                }
                await Task.Delay(200, ct);
            }

            return false;
        }

        private static void TryStartBrowser()
        {
            string? browserPath = FindBrowserPath();

            if (browserPath == null)
                throw new Exception("Не удалось найти поддерживаемый браузер в системе. Для работы автоматизации нужен Chrome или Edge");

            string profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OzonBrowserProfile");
            Directory.CreateDirectory(profilePath);

            ProcessStartInfo psi = new()
            {
                FileName = browserPath,
                UseShellExecute = false,
            };

            psi.ArgumentList.Add("--remote-debugging-port=9222");
            psi.ArgumentList.Add($"--user-data-dir={profilePath}");

            if(Process.Start(psi) == null)
                throw new Exception("Не удалось запустить процесс браузера");
        }

        private static string? FindBrowserPath()
        {
            string?[] candidates = new string?[]
            {
                ReadAppPath("chrome.exe"), ReadAppPath("msedge.exe"),
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            };

            return candidates.FirstOrDefault(p => p is not null && File.Exists(p));
        }

        private static string? ReadAppPath(string exeName)
        {
            foreach (var root in new[] { "HKEY_LOCAL_MACHINE", "HKEY_CURRENT_USER" })
            {
                if (Registry.GetValue(@$"{root}\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}", null, null) is string p
                    && !string.IsNullOrEmpty(p))
                    return p;
            }
            return null;
        }

        private async Task OpenAsync(string url)
        {
            await Page.GotoAsync(url);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.GetByRole(AriaRole.Banner).First.WaitForAsync(new() { Timeout = 10000 });
        }

        private static decimal? ParsePrice(in JsonElement root)
        {
            decimal? result = null;

            if(root.TryGetProperty("offers", out var offers))
            {
                var stringPrice = offers.TryGetProperty("price", out var priceElement) ? priceElement.GetString() : null;
                result = decimal.TryParse(stringPrice,
                NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal p) ? p : null;
            }

            return result;
        }

        public async ValueTask DisposeAsync()
        {
            if(_browser != null) await _browser.DisposeAsync();
            if (_pw != null) _pw.Dispose();
        }
    }
}
