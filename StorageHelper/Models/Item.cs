//TODO: доделать vm, сделать сохранение в бд при смене в интерфейсе
namespace StorageHelper.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }
        public int ParLevel { get; set; }
        public int CurrentOnStorage { get; set; }
        public bool IsActive { get; set; }

        public List<PriceRecord> PriceRecords { get; } = new();
    }
}
