namespace StorageHelper.Models
{
    public class PriceRecord
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public DateTime CapturedAt { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }
    }
}
