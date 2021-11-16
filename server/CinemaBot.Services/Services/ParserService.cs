using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using CinemaBot.Core;
using CinemaBot.Data;
using CinemaBot.Data.Entites;
using CinemaBot.Data.Repositories;
using CinemaBot.Data.Repositories.Interfaces;
using CinemaBot.Models;
using CinemaBot.Services.Interfaces;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using static System.Convert;

namespace CinemaBot.Services.Services
{
    public class ParserService : IParserService
    {
        private readonly ILogger _log;
        private readonly IMapper _mapper;
        private readonly bool _useProxy;
        private readonly ProxyService _serviceProxy;
        private Proxy _currentProxy;
        private readonly int[] _exceptionIds;
        private const int MaxCount = 3;
        private readonly IUrlRepository _urlRepository;
        private readonly TelegramService _telegram;

        public ParserService(ILogger log, IConfiguration configuration, IMapper mapper, TelegramService telegram)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _log = log;
            _mapper = mapper;
            _telegram = telegram;

            var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .Options;
            ApplicationDbContext context = new ApplicationDbContext(contextOptions);
            context.Database.Migrate();
            _urlRepository = new UrlRepository(context);

            _useProxy = ToBoolean(configuration["useProxy"]);
            _exceptionIds = configuration.GetSection("exceptionIds").Get<int[]>();

            if (_useProxy)
            {
                _serviceProxy = new ProxyService();
                _currentProxy = _serviceProxy.GetRandomProxy() ?? throw new Exception("The proxy list is empty");
            }
        }

        public async void Parser(string url)
        {
            await _telegram.GetBotClientAsync();

            int[] ids = MainPageParser(url);
            var resultIds = await CheckIds(ids);

            if (resultIds.Length != 0)
            {
                List<UrlModel> links = await SecondPagesParser(resultIds);

                SaveToLog(links);

                await _telegram.SendMessageMovies(links);

                await SaveUrls(links);
            }

            _telegram.Cts.Cancel();
            _log.Information("Parsing completed");
        }

        private int[] MainPageParser(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new Exception("The \"url\" value is empty");

            _log.Information("Parse url: {0}", url);

            int i = 0;
            bool isStarting = false;
            do
            {
                try
                {
                    HtmlWeb web = new HtmlWeb();

                    var doc = _useProxy
                        ? web.Load(url, _currentProxy.ProxyHost, _currentProxy.ProxyPort, _currentProxy.UserId,
                            _currentProxy.Password)
                        : web.Load(url);

                    if (doc == null) return null;
                    var nodes = doc.DocumentNode.SelectNodes("//a[@class='topictitle']");

                    int count = nodes.Count;
                    if (count > 0)
                    {
                        int[] ids = new int[count];

                        for (int j = 0; j < count; j++)
                            ids[j] = GetParamFromUrl(nodes[j].Attributes["href"].Value);

                        return ids.Except(_exceptionIds).ToArray();
                    }

                    throw new Exception("The parsing result is empty");
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    
                    if (ex is WebException)
                    {
                        if (_useProxy)
                        {
                            i++;
                            _log.Error("{0} is bad", propertyValue: _currentProxy.ProxyHost);
                            isStarting = true;
                            if (i == _serviceProxy.Count)
                            {
                                _serviceProxy.SaveProxy();
                                throw new Exception("Link " + url + " loading failed");
                            }

                            _currentProxy = _serviceProxy.GetRandomProxy() ??
                                            throw new Exception("The proxy list is empty");
                        }
                    }

                    
                }
            } while (isStarting);

            return null;
        }

        private async Task<List<UrlModel>> SecondPagesParser(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                _log.Error("The array of identifiers for the second-level page analyzer is empty");
                return null;
            }

            var tasks = new List<Task>();

            try
            {
                foreach (var id in ids)
                    tasks.Add(Task.Run(() => GetUrl(id)));

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }

            List<UrlModel> results = new List<UrlModel>();
            foreach (var task in tasks)
            {
                var result = ((Task<UrlModel>) task).Result;
                if (result != null)
                    results.Add(result);
            }

            return results;
        }

        private UrlModel GetUrl(int id)
        {
            var proxy = _useProxy
                ? _serviceProxy.GetRandomProxy() ?? throw new Exception("The proxy list is empty")
                : null;
            int i = 0;
            bool isStarting = false;
            var url = Constants.NnmClubTopic + "?t=" + Convert.ToString(id);
            HtmlWeb web = new HtmlWeb();

            do
            {
                try
                {
                    var doc = _useProxy
                        ? web.Load(url, proxy.ProxyHost, proxy.ProxyPort, proxy.UserId,
                            proxy.Password)
                        : web.Load(url);

                    string title = doc.DocumentNode.SelectSingleNode("//a[@class='maintitle']").InnerText;
                    var nodeImg = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

                    string imgUrl = "";
                    if (nodeImg != null)
                        imgUrl = nodeImg.Attributes["content"].Value;
                    else
                    {
                        HtmlNodeCollection nodesImg =
                            doc.DocumentNode.SelectNodes("//var[@class='postImg postImgAligned img-right']");

                        if (nodesImg != null)
                        {
                            HtmlNode imgNode = nodesImg[0];
                            string titleImg = imgNode.Attributes["title"].Value;
                            string linkStr = "?link=";
                            int index = titleImg.IndexOf(linkStr, StringComparison.Ordinal) + linkStr.Length;
                            imgUrl = titleImg.Substring(index, titleImg.Length - index);
                        }
                    }

                    var urlModel = new UrlModel(id, title, imgUrl);
                    return urlModel;
                }
                catch (Exception ex)
                {
                    if (_useProxy)
                    {
                        i++;
                        isStarting = true;
                        if (i == MaxCount)
                        {
                            _log.Error("Link {0} loading failed", url);
                            return null;
                        }

                        proxy = _serviceProxy.GetRandomProxy() ??
                                throw new Exception("The proxy list is empty");
                    }
                    else
                    {
                        _log.Error("Link {0} loading failed", url);
                        _log.Error(ex.Message);
                        return null;
                    }
                }
            } while (isStarting);

            return null;
        }

        private int GetParamFromUrl(string url)
        {
            Uri myUri = new Uri(Constants.NnmClub + url);
            string param = HttpUtility.ParseQueryString(myUri.Query).Get("t");
            return ToInt32(param);
        }

        private async Task SaveUrls(List<UrlModel> urls)
        {
            if (urls == null) return;

            try
            {
                List<Url> links = urls.Select(url => _mapper.Map<Url>(url)).ToList();
                await _urlRepository.InsertRangeAsync(links);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        private async Task<int[]> CheckIds(int[] ids)
        {
            if (ids == null || ids.Length == 0) return null;

            try
            {
                var urls = await _urlRepository.FindAllByWhereAsync(url => ids.Contains(url.Id));
                var dbIds = urls.Select(url => url.Id).ToArray();
                if (dbIds.Length == 0) return ids;
                return ids.Except(dbIds).ToArray();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }

            return null;
        }

        private void SaveToLog(List<UrlModel> urls)
        {
            urls.ForEach((url) => _log.Information(url.ToString()));
        }
    }
}