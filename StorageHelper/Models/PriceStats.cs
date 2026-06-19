namespace StorageHelper.Models
{
    public record class PriceStats
    {
        public decimal? Current {  get; set; }
        public decimal? Previous { get; set; }
        public decimal? Minimum { get; set; }
    }
}
