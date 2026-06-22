namespace StorageHelper.Models
{
    public record class ReviewLine(string Name, string Sku, int Quantity, decimal? CurrentPrice, decimal? PriceChangesPercent)
    {
        public decimal? LineTotal => Quantity * CurrentPrice;
    }
}
