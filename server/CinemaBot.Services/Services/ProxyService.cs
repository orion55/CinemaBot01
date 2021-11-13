using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CinemaBot.Services.Services
{
    public class ProxyService
    {
        const string ProxyFilename = "proxy.txt";
        const string BadProxyFilename = "proxy_bad.txt";

        private Proxy[] _proxies;
        public int Count { get; private set; }

        public ProxyService()
        {
            Init();
        }

        private void Init()
        {
            string proxyFullname = Path.Combine(Directory.GetCurrentDirectory(), ProxyFilename);

            if (!File.Exists(proxyFullname))
            {
                throw new Exception($"File {proxyFullname} not found");
            }

            Count = File.ReadLines(proxyFullname).Count();
            _proxies = new Proxy[Count];

            using StreamReader stream = new StreamReader(proxyFullname, Encoding.Default);
            string line = "";
            int i = 0;
            while ((line = stream.ReadLine()) != null)
            {
                if (!String.IsNullOrEmpty(line))
                {
                    string[] parts = line.Split(':');
                    _proxies[i] = new Proxy
                    {
                        Id = Guid.NewGuid(),
                        ProxyHost = parts[0],
                        ProxyPort = Convert.ToInt32(parts[1]),
                        UserId = (parts.Length > 2 && !String.IsNullOrEmpty(parts[2])) ? parts[2] : String.Empty,
                        Password = (parts.Length > 3 && !String.IsNullOrEmpty(parts[3])) ? parts[3] : String.Empty,
                        IsBad = false
                    };
                    i++;
                }
            }
        }

        public Proxy GetRandomProxy()
        {
            var proxyList = _proxies.Where(proxy => !proxy.IsBad);
            if (!proxyList.Any()) return null;

            Random random = new Random();
            int index = random.Next(0, _proxies.Length);
            return _proxies[index];
        }

        public void SetBadProxy(Guid Id)
        {
            var proxy = _proxies.FirstOrDefault(proxy => proxy.Id == Id);
            if (proxy != null) proxy.IsBad = true;
        }

        public void SaveProxy()
        {
            var proxyListBad = _proxies.Where(proxy => proxy.IsBad);
            IEnumerable<Proxy> listBad = proxyListBad as Proxy[] ?? proxyListBad.ToArray();
            if (listBad.Any())
            {
                SaveFileProxy(listBad, BadProxyFilename, false);

                var proxyListGood = _proxies.Where(proxy => !proxy.IsBad);
                IEnumerable<Proxy> listGood = proxyListGood as Proxy[] ?? proxyListGood.ToArray();
                SaveFileProxy(listGood, ProxyFilename, true);
            }
        }

        private void SaveFileProxy(IEnumerable<Proxy> list, string filename, bool isDelete)
        {
            string proxyFullname = Directory.GetCurrentDirectory() + "\\" + filename;
            if (isDelete && File.Exists(proxyFullname))
                File.Delete(proxyFullname);

            using StreamWriter stream = new StreamWriter(proxyFullname, !isDelete, Encoding.Default);
            foreach (Proxy proxy in list)
                stream.WriteLine(proxy);
        }
    }
}