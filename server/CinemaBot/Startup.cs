using System;
using System.Collections.Generic;
using System.IO;
using AutoMapper;
using CinemaBot.Configurations;
using CinemaBot.Data;
using CinemaBot.Services.Interfaces;
using CinemaBot.Services.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.SystemConsole.Themes;

namespace CinemaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private ILogger Logger { get; }

        private IMapper _mapper;

        public Startup()
        {
            var configurationPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");
            var builder = new ConfigurationBuilder()
                .AddJsonFile(configurationPath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            _configuration = builder.Build();

            string tableName = "logs";

            IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
            {
                {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text)},
                {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text)},
                {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar)},
                {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp)},
                {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text)},
                {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb)},
                {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb)},
                {
                    "machine_name",
                    new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l")
                }
            };

            string connectionstring = _configuration.GetConnectionString("DefaultConnection");

            Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.PostgreSQL(connectionstring, tableName, columnWriters, LogEventLevel.Information,
                    null, null, 30, null, true, "", true, false)
                .CreateLogger();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient(provider => _configuration);

            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseNpgsql(_configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // services.AddScoped<IUrlRepository, UrlRepository>();

            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection")));
            GlobalConfiguration.Configuration
                .UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection"))
                .WithJobExpirationTimeout(TimeSpan.FromDays(7));
            services.AddHangfireServer();

            var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new MappingProfile()); });

            _mapper = mapperConfig.CreateMapper();
            services.AddSingleton(_mapper);

            services.AddTransient<TelegramService>();
            services.AddTransient<IParserService, ParserService>();

            services.AddSingleton(Logger);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IParserService parserService,
            IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient
        )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var options = new DashboardOptions
            {
                Authorization = new[]
                {
                    new DashboardAuthorization(new[]
                    {
                        new HangfireUserCredentials
                        {
                            Username = _configuration.GetSection("HangfireCredentials:UserName").Value,
                            Password = _configuration.GetSection("HangfireCredentials:Password").Value
                        }
                    })
                }
            };
            app.UseHangfireDashboard("/hangfire", options);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello World");
                });
            });

            Job jobscheduler = new Job(Logger, _configuration, parserService);
            // backgroundJobClient.Enqueue(() => jobscheduler.Run());
            recurringJobManager.AddOrUpdate("Runs Every 1 Hour", () => jobscheduler.Run(), "0 * * * *");
        }
    }
}