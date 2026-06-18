namespace StorageHelper.Models
{
    public struct PasswordData
    {
        public string Hash {  get; set; }
        public string Salt { get; set; }
    }

    public class AppSettings
    {
        public string ConnectionString { get; set; } = "Data Source=Storage.db";
        public PasswordData? Password { get; set; }   
    }
}
