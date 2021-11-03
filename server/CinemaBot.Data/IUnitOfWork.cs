using System;
using CinemaBot.Data.Repositories.Interfaces;

namespace CinemaBot.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IUrlRepository Urls { get; }
        void Save();
    }
}