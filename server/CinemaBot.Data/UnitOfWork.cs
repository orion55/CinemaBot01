using System;
using CinemaBot.Data.Repositories;
using CinemaBot.Data.Repositories.Interfaces;

namespace CinemaBot.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IUrlRepository _urlRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUrlRepository Urls => _urlRepository ??= new UrlRepository(_context);

        public void Save()
        {
            _context.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }

            _disposed = true;
        }
    }
}