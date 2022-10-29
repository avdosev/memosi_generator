using MemesApi.Db.Models;
using Microsoft.EntityFrameworkCore;

namespace MemesApi.Db
{
    public class MemeContext: DbContext
    {

        public MemeContext() : base() { }
        public MemeContext(DbContextOptions<MemeContext> options) : base(options)
        {
        }

        public DbSet<MemeFile> Files { get; set; } 
        public DbSet<Estimate> Estimates { get; set; }
    }
}
