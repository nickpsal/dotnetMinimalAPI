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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //pass data to Books Table
            var Books = new List<Book>()
            {
                new Book
                {
                    Id = 1,
                    Title = "Book 1",
                    Author = "Author 1"
                },

                new Book
                {
                    Id = 2,
                    Title = "Book 2",
                    Author = "Author 2"
                },

                new Book
                {
                    Id = 3,
                    Title = "Book 3",
                    Author = "Author 3"
                },

                new Book
                {
                    Id = 4,
                    Title = "Book 4",
                    Author = "Author 4"
                },

                new Book
                {
                    Id = 5,
                    Title = "Book 5",
                    Author = "Author 5"
                },
            };
            modelBuilder.Entity<Book>().HasData(Books);

            var Users = new List<User>()
            {
                new User
                {
                    Id = 1,
                    Username = "nickpsal",
                    Email = "nickpsal@gmail.com",
                    Role = "Admin",
                    Password = "$2a$11$46rxZhpmF0bNNEM2s/peeuUmsP/zF0pxzCnQz6AswWNJZPjcN60K6"
                }
            };
            modelBuilder.Entity<User>().HasData(Users);
        }
    }
}
