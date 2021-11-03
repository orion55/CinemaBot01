using CinemaBot.Data.Entites;
using CinemaBot.Data.Repositories.Interfaces;

namespace CinemaBot.Data.Repositories
{
    public class UrlRepository : RepositoryBase<Url>, IUrlRepository
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ApplicationDbContext _context;

        public UrlRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Can bee extended by any additional methods that do not present in RepositoryBase
    }
}