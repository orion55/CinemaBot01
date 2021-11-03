using System.ComponentModel.DataAnnotations;

namespace CinemaBot.Data.Entites
{
    public class Url
    {
        [Key] public int Id { get; set; }

        public string Title { get; set; }

        public string ImgUrl { get; set; }
    }
}