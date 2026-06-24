namespace StorageHelper.Models
{
    public record class PasswordData(string Hash, string Salt);

    public class AppSettings
    {
        public string ConnectionString { get; set; } = "Data Source=Storage.db";
        public PasswordData? Password { get; set; }
        public bool FakeAutomation { get; set; } = false;
    }
}
