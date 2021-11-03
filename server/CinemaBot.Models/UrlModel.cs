using System;
using CinemaBot.Core;

namespace CinemaBot.Models
{
    public class UrlModel
    {
        public UrlModel(int id, string title, string imgUrl)
        {
            Id = id;
            Title = title;
            ImgUrl = imgUrl;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public string ImgUrl { get; set; }

        public string UrlId()
        {
            return Constants.NnmClubTopic + "?t=" + Convert.ToString(Id);
        }

        public override string ToString()
        {
            return UrlId() + " : " + Title + " : " + ImgUrl;
        }
    }
}