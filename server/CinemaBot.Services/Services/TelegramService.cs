using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaBot.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CinemaBot.Services.Services
{
    public class TelegramService 
    {
        private readonly ILogger _log;
        private ITelegramBotClient _botClient = null;
        private readonly IConfiguration _config;
        public CancellationTokenSource Cts{ get; private set; }

        public TelegramService(ILogger log, IConfiguration configuration)
        {
            _log = log;
            _config = configuration;
        }

        public async Task<ITelegramBotClient> GetBotClientAsync()
        {
            if (_botClient != null)
                return _botClient;

            _botClient = new TelegramBotClient(_config["TelegramToken"]);
            var me = await _botClient.GetMeAsync();

            Cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                Cts.Token);

            _log.Information("Start listening for @{0}", me.Username);
            return _botClient;
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.ToString()
            };

            _log.Error(ErrorMessage);
            return Task.CompletedTask;
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;
            if (update.Message.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;

            _log.Information("Received a '{0}' message in chat {1}.", update.Message.Text, chatId);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + update.Message.Text
            );
        }
        public async Task SendMessageMovies(List<UrlModel> urls)
        {
            if (urls == null) return;

            try
            {
                foreach (var url in urls)
                {
                    var urlId = url.UrlId();
                    var caption = $"{url.Title}\n\n<a href=\"{urlId}\">{urlId}</a>"; 
                    await _botClient.SendPhotoAsync(
                        chatId: _config["TelegramChatId"],
                        photo: url.ImgUrl,
                        caption: caption,
                        parseMode: ParseMode.Html
                    );
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
            
        }
    }
}