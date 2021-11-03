using CinemaBot.Data.Entites;
using Microsoft.EntityFrameworkCore;

namespace CinemaBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Url> Urls { get; set; }
    }
}