using System;

namespace CinemaBot.Services.Services
{
    public class Proxy
    {
        public Guid Id { get; set; }
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public bool IsBad { get; set; }

        public override string ToString()
        {
            string result = ProxyHost + ":" + Convert.ToString(ProxyPort);
            if (!String.IsNullOrEmpty(UserId) && !String.IsNullOrEmpty(Password))
                result += ":" + UserId + ":" + Password;
            return result;
        }
    }
}