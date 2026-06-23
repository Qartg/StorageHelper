using StorageHelper.Models;

namespace StorageHelper.Services.Automation
{
    public interface IVendorAutomation
    {
        Task<GetItemInfoResult> GetItemInfo(string sku, CancellationToken ct = default);
        Task<AddToCartResult> AddItemToCart(string sku, int quantity, CancellationToken ct = default);
    }
}
