using System;
using CinemaBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CinemaBot
{
    public class Job
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IParserService _parser;

        public Job(ILogger logger, IConfiguration config, IParserService parserService)
        {
            _logger = logger;
            _config = config;
            _parser = parserService;
        }

        public void Run()
        {
            string[] urls = _config.GetSection("urls").Get<string[]>();
            try
            {
                foreach (var url in urls)
                    _parser.Parser(url);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
        }
    }
}