namespace MemesApi.Db.Models
{
    public class Estimate
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public int FileId { get; set; }
        public string? ClientId { get; set; }
    }
}
