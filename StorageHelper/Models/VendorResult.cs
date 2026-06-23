namespace StorageHelper.Models
{
    public record class AddToCartResult(string Sku, bool Success, string Message);
    public record class GetItemInfoResult(string Sku, 
        bool Success, string? Name, string? Description, 
        string? Vendor, string? ImageURL, bool? IsOrderable, decimal? Price, string Message);
}
