using AutoMapper;
using CinemaBot.Data.Entites;
using CinemaBot.Models;

namespace CinemaBot.Configurations
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Url, UrlModel>();
            CreateMap<UrlModel, Url>();
        }
    }
}