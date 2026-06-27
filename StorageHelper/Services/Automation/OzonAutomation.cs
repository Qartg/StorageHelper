using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using StorageHelper.Models;
using System.Globalization;
using System.Text.Json;


namespace StorageHelper.Services.Automation
{
    public record class OzonLd(string? Sku, string? Name, string? Description, decimal? Price, string? Image);
        
    public class OzonAutomation : IVendorAutomation
    {
        private readonly ILogger<OzonAutomation> _logger;
        private readonly IBrowserSession _browserSession;
        private readonly AppSettings _settings;
        private bool _loggedIn;

        public IPage Page => _browserSession.Page;

        public OzonAutomation(ILogger<OzonAutomation> logger, IBrowserSession browserSession, AppSettings settings)
        {
            _logger = logger;
            _browserSession = browserSession;

            _browserSession.Disconnected += () => _loggedIn = false;
            _settings = settings;
        }

        public async Task<AddToCartResult> AddItemToCart(string sku, int quantity, CancellationToken ct = default)
        {
            try
            {
                if (!await EnsureReadyAsync(ct))
                    return new(sku, false, "Произошла ошибка при открытии браузера");

                if (!Page.Url.Contains(sku))
                    await OpenAsync(OzonConstants.Product(sku));

                var ld = await Page.Locator(OzonConstants.JsonLdScript).First.TextContentAsync();
                if (ld == null)
                    throw new Exception("ld оказался пустым");

                string? name = ParseLd(ld).Name;
                if(name == null)
                    return new(sku, false, "Не удалось получить имя товара");

                await EnsureItemInCart(sku);
                await OpenAsync(OzonConstants.Cart);

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
                if(!await EnsureReadyAsync(ct))
                    return new(sku, false, null, null, null, null, null, null, "Произошла ошибка при открытии браузера");

                await OpenAsync(OzonConstants.Product(sku));

                var ld = await Page.Locator(OzonConstants.JsonLdScript).First.TextContentAsync();
                if (ld == null)
                    throw new Exception("ld оказался пустым");

                var pLd = ParseLd(ld);
                if (pLd.Sku == null || pLd.Sku != sku)
                    return new(sku, false, null, null, null, null, null, null, "SKU не совпадают");

                var vendorLocator = Page.Locator(OzonConstants.VendorLink).First;
                var vendor = await vendorLocator.CountAsync() > 0 ? await vendorLocator.GetAttributeAsync("title") : null;

                bool outOfStock = await Page.Locator(OzonConstants.OutOfStockBlock).First.CountAsync() > 0;

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
                    .Locator(OzonConstants.QuantityInputAncestor)
                    .Locator(OzonConstants.QuantityInput);
            await qty.WaitForAsync();

            if(int.TryParse(await qty.InputValueAsync(), out var p) && p == quantity) 
                return p;

            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await qty.FillAsync(quantity.ToString());
                await qty.PressAsync("Tab");
            }, resp => resp.Url.Contains(OzonConstants.CartSummaryResponse) && resp.Status == 200);

            return int.TryParse(await qty.InputValueAsync(), out p) ? p : null;
        }

        private async Task EnsureItemInCart(string sku)
        {
            var toCartWidgetLocator = Page.Locator(OzonConstants.AddToCartWidget);
            await toCartWidgetLocator.First.WaitForAsync();

            var toCartButton = toCartWidgetLocator.First.GetByRole(AriaRole.Button, new() { Name = OzonConstants.AddToCartButtonText });
            if (await toCartButton.CountAsync() > 0)
            {
                await Page.RunAndWaitForResponseAsync(async () =>
                {
                    await toCartButton.ClickAsync();
                }, resp => resp.Url.Contains(OzonConstants.AddToCartResponse) && resp.Status == 200);
            }

            var inCartBtn = toCartWidgetLocator.First.GetByRole(AriaRole.Button, new() { Name = OzonConstants.InCartButtonText });
            await inCartBtn.WaitForAsync();
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

        private async Task EnsureLoggedInAsync(CancellationToken ct = default, IProgress<AuthPhase>? progress = null)
        {
            await OpenAsync(OzonConstants.Home);

            var loginBtn = Page.Locator(OzonConstants.AnonymousProfileMenu).First;

            if (await loginBtn.CountAsync() > 0)
            {
                await loginBtn.ClickAsync();
                _logger.LogInformation("Жду авторизации на сайте");
                progress?.Report(AuthPhase.AwaitingLogin);
                var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                
                while (deadline > DateTime.UtcNow)
                {
                    if (await loginBtn.CountAsync() == 0)
                    {
                        progress?.Report(AuthPhase.Ready);
                        _loggedIn = true;
                        return;
                    }

                    await Task.Delay(1000, ct);
                }

                throw new Exception("Превышено время ожидания при авторизации в аккаунт");
            }
            _loggedIn = true;
        }


        private async Task OpenAsync(string url)
        {
            await Page.GotoAsync(url);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.GetByRole(AriaRole.Banner).First.WaitForAsync();
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

        public async Task<bool> ConnectAsync(IProgress<AuthPhase> progress, CancellationToken ct = default) => await EnsureReadyAsync(ct, progress);

        private async Task<bool> EnsureReadyAsync(CancellationToken ct = default, IProgress<AuthPhase>? progress = null)
        {
            if (!await _browserSession.EnsureStartedAsync(OzonConstants.Home, ct))
                return false;

            if (_settings.RequireLoggingIn && !_loggedIn)
                await EnsureLoggedInAsync(ct, progress);

            return true;
        }
    }
}
