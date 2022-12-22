namespace MemesApi.Db.Models
{
    public class FileMeta
    {
        public int Id { get; set; }
        public string? Format { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
