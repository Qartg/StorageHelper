using Microsoft.Extensions.Logging;
using StorageHelper.Models;

namespace StorageHelper.Services.Automation
{
    public class FakeVendorAutomation : IVendorAutomation
    {
        private List<string> _names = new()
        {
            "Шприц трехкомпонентный одноразовый 5 мл с иглой 22G",
            "Пинцет анатомический общего назначения 150 мм",
            "Пульсоксиметр напалечный медицинский MD300C12",
            "Бинт марлевый медицинский стерильный 5м х 10см",
            "Кожный антисептик для обработки рук и поверхностей 1 л",
        };

        private ILogger<FakeVendorAutomation> _logger;

        public FakeVendorAutomation(ILogger<FakeVendorAutomation> logger)
        {
            _logger = logger;
        }

        public async Task<AddToCartResult> AddItemToCart(string sku, int quantity, CancellationToken ct = default)
        {
            await Task.Delay(Random.Shared.Next(5000, 6200), ct);

            if(Random.Shared.Next(5) == 0)
            {
                _logger.LogWarning("Error add item to cart, {sku}", sku);
                return new AddToCartResult(sku, false, "Добавление в корзину не удалось");
            }

            return new AddToCartResult(sku, true, "Добавление в корзину успешно");
        }

        public Task<bool> ConnectAsync(IProgress<AuthPhase> progress, CancellationToken ct = default)
        {
            progress.Report(AuthPhase.Ready); 
            return Task.FromResult(true);
        }

        public async Task<GetItemInfoResult> GetItemInfo(string sku, CancellationToken ct = default)
        {
            await Task.Delay(Random.Shared.Next(800, 1200), ct);

            if (Random.Shared.Next(5) == 0)
            {
                _logger.LogWarning("Error GetItemInfo, {sku}", sku);
                return new GetItemInfoResult(sku, false, null, null, null, null, null, null, "Получение актуальной информации о товаре не удалось");
            }

            return new GetItemInfoResult(sku, true, _names[Random.Shared.Next(0, _names.Count)], "Some desc", "Some vendor",
                null, Random.Shared.Next(2)==0, Random.Shared.Next(50000), "Получение актуальной информации о товаре удалось");
        }
    }
}
