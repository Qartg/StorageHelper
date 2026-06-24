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
    public record class OzonLd(string? Sku, string? Name, string? Description, decimal? Price, string? Image);
        
    public class OzonAutomation : IVendorAutomation, IAsyncDisposable
    {
        private readonly ILogger<OzonAutomation> _logger;
        private IPlaywright? _pw;
        private IBrowser? _browser;
        private IPage? _page;
        private bool _ready;

        public IPage Page => _page ?? throw new InvalidOperationException("Страница не загружена");

        private const string OZON_BASE_LINK = "https://www.ozon.ru/";

        private readonly string ProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OzonBrowserProfile");

        public OzonAutomation(ILogger<OzonAutomation> logger)
        {
            _logger = logger;
        }

        public async Task<AddToCartResult> AddItemToCart(string sku, int quantity, CancellationToken ct = default)
        {
            try
            {
                if (!await EnsureStartedAsync(ct))
                    return new(sku, false, "Произошла ошибка при открытии браузера");

                if (!Page.Url.Contains($"{sku}/?oos_search=false"))
                    await OpenAsync($"{OZON_BASE_LINK}product/{sku}/?oos_search=false");

                var ld = await Page.Locator("script[type='application/ld+json']").First.TextContentAsync();
                if (ld == null)
                    throw new Exception("ld оказался пустым");

                string? name = ParseLd(ld).Name;
                if(name == null)
                    return new(sku, false, "Не удалось получить имя товара");

                await EnsureItemInCart(sku);
                await OpenAsync($"{OZON_BASE_LINK}cart");

                int? finalInCart = await SetQuantityInCart(name, quantity);
                
                if (finalInCart != null)
                    return new(sku, true, $"Успешно добавил {finalInCart} в корзину");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось добавить предмет в корзину: {sku}", sku);
            }

            return new(sku, false, "Произошла ошибка при добавлении товара в корзину");
        }

        public async Task<GetItemInfoResult> GetItemInfo(string sku, CancellationToken ct = default)
        {
            try
            {
                if(!await EnsureStartedAsync(ct))
                    return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при открытии браузера");

                await OpenAsync($"{OZON_BASE_LINK}product/{sku}/?oos_search=false");

                var ld = await Page.Locator("script[type='application/ld+json']").First.TextContentAsync();
                if (ld == null)
                    throw new Exception("ld оказался пустым");

                var pLd = ParseLd(ld);
                if (pLd.Sku == null || pLd.Sku != sku)
                    return new(sku, false, null, null, null, null, null, null, "SKU не совпадают");

                var vendorLocator = Page.Locator("a[href*='/seller/'][title]").First;
                var vendor = await vendorLocator.CountAsync() > 0 ? await vendorLocator.GetAttributeAsync("title") : null;

                bool outOfStock = await Page.Locator("[data-widget='webOutOfStock']").First.CountAsync() > 0;

                string msg = outOfStock ? "Товара нет в наличии" : "Успешно получил информацию о товаре";
                return new(sku, true, pLd.Name, pLd.Description, vendor, pLd.Image, !outOfStock, pLd.Price, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить информацию о предмете: {sku}", sku);
            }

            return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при получении информации о товаре");
        }

        private async Task<int?> SetQuantityInCart(string name, int quantity)
        {
            var qty = Page.GetByText(name).First
                    .Locator("xpath=ancestor::*[.//input[@inputmode='decimal']][1]")
                    .Locator("input[inputmode='decimal']");
            await qty.WaitForAsync(new() { Timeout = 10000 });

            if(int.TryParse(await qty.InputValueAsync(), out var p) && p == quantity) 
                return p;

            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await qty.FillAsync(quantity.ToString());
                await qty.PressAsync("Tab");
            }, resp => resp.Url.Contains("_action/summary") && resp.Status == 200);

            return int.TryParse(await qty.InputValueAsync(), out p) ? p : null;
        }

        private async Task EnsureItemInCart(string sku)
        {
            var toCartWidgetLocator = Page.Locator("[data-widget='webAddToCart']:visible");
            await toCartWidgetLocator.First.WaitForAsync(new() { Timeout = 10000 });

            var toCartButton = toCartWidgetLocator.First.GetByRole(AriaRole.Button, new() { Name = "В корзину" });
            if (await toCartButton.CountAsync() > 0)
            {
                await Page.RunAndWaitForResponseAsync(async () =>
                {
                    await toCartButton.ClickAsync();
                }, resp => resp.Url.Contains("addToCart") && resp.Status == 200);
            }

            var inCartBtn = toCartWidgetLocator.First.GetByRole(AriaRole.Button, new() { Name = "В корзине" });
            await inCartBtn.WaitForAsync(new() { Timeout = 10000 });
            bool inCart = await inCartBtn.CountAsync() > 0;

            if (!inCart)
                throw new Exception("Не удалось добавить предмет в корзину");
        }

        private static OzonLd ParseLd(string ld)
        {
            using var doc = JsonDocument.Parse(ld);
            var root = doc.RootElement;

            var sku = root.TryGetProperty("sku", out var v) ? v.GetString() : null;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
            var price = ParsePrice(in root);
            var image = root.TryGetProperty("image", out var i) ? i.GetString() : null;

            return new OzonLd(sku, name, desc, price, image);
        }

        private async Task<bool> EnsureStartedAsync(CancellationToken ct = default, IProgress<AuthPhase>? progress = null)
        {
            try
            {
                if (_ready && _page != null) return true;
                _pw ??= await Playwright.CreateAsync();

                if(await WaitForCDPAsync(ct, 2.5) == null)
                    TryStartBrowser();

                if (await WaitForCDPAsync(ct) is int port)
                {
                    _browser ??= await _pw.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}");
                    var ctx = _browser.Contexts.Count > 0 ? _browser.Contexts[0] : await _browser.NewContextAsync();
                    _page = ctx.Pages.Count > 0 ? ctx.Pages[0] : await ctx.NewPageAsync();
                    _browser.Disconnected += BrowserDisconnected;
                    ctx.SetDefaultTimeout(10000f);

                    await EnsureLoggedInAsync(ct, progress);
                    _ready = true;

                    return true;
                }
                else
                    _logger.LogError("CDP не поднялся в течение 15 секунд");

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

        private void BrowserDisconnected(object? sender, IBrowser e)
        {
            _ready = false; 
            _page = null;
            _browser = null;
        }

        private async Task EnsureLoggedInAsync(CancellationToken ct = default, IProgress<AuthPhase>? progress = null)
        {
            await OpenAsync(OZON_BASE_LINK);

            var loginBtn = Page.GetByText("Войти", new() { Exact = true }).First;

            if (await loginBtn.IsVisibleAsync())
            {
                await loginBtn.ClickAsync();
                _logger.LogInformation("Жду авторизации на сайте");
                progress?.Report(AuthPhase.AwaitingLogin);
                var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                
                while (deadline > DateTime.UtcNow)
                {
                    if (!await loginBtn.IsVisibleAsync())
                    {
                        progress?.Report(AuthPhase.Ready);
                        return;
                    }

                    await Task.Delay(1000, ct);
                }

                throw new Exception("Превышено время ожидания при авторизации в аккаунт");
            }
        }

        private async Task<int?> WaitForCDPAsync(CancellationToken ct = default, double waitTime = 15)
        {
            using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(waitTime);

            while(DateTime.UtcNow < deadline)
            {
                await Task.Delay(200, ct);
                try
                {
                    ct.ThrowIfCancellationRequested();

                    int? port = await ReadPortFromFile();
                    if (port == null)
                        continue;

                    var response = await http.GetAsync($"http://127.0.0.1:{port}/json/version", ct);
                    if (response.IsSuccessStatusCode)
                        return port;
                }
                catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
                {
                    if (ct.IsCancellationRequested) return null;
                }
            }

            return null;
        }

        private async Task<int?> ReadPortFromFile()
        {
            string filePath = Path.Combine(ProfilePath, "DevToolsActivePort");
            if (File.Exists(filePath))
            {
                using (var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sReader = new StreamReader(fStream))
                {
                    string? line;

                    if ((line = await sReader.ReadLineAsync()) != null)
                        if (int.TryParse(line, out var port))
                            return port;
                }
            }
            return null;
        }

        private void TryStartBrowser()
        {
            string? browserPath = FindBrowserPath();

            if (browserPath == null)
                throw new Exception("Не удалось найти поддерживаемый браузер в системе. Для работы автоматизации нужен Chrome или Edge");

            Directory.CreateDirectory(ProfilePath);

            ProcessStartInfo psi = new()
            {
                FileName = browserPath,
                UseShellExecute = false,
            };

            psi.ArgumentList.Add("--remote-debugging-port=0");
            psi.ArgumentList.Add($"--user-data-dir={ProfilePath}");

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
                    return p.Trim('"');
            }
            return null;
        }

        private async Task OpenAsync(string url)
        {
            await Page.GotoAsync(url);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

        public Task<bool> ConnectAsync(IProgress<AuthPhase> progress, CancellationToken ct = default) => EnsureStartedAsync(ct, progress);
    }
}
