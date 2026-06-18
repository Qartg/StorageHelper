namespace StorageHelper.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? Vendor { get; set; }
        public string? ImageURL { get; set; }
        public int ParLevel { get; set; }
        public int CurrentOnStorage { get; set; }
        public bool IsActive { get; set; }
        public bool IsOredrable { get; set; }

        public List<PriceRecord> PriceRecords { get; } = new();
    }
}
