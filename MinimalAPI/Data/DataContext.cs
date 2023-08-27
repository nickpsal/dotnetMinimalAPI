using MinimalAPI.Models.Domain;

namespace MinimalAPI.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer("Server=.\\sqlexpress;Database=minimalAPIdb;Trusted_Connection=true;TrustServerCertificate=true;");
        }

        public DbSet<Book> Books => Set<Book>();
        public DbSet<User> Users => Set<User>();
    }
}
