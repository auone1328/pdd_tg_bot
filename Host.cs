using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramPDDBot
{
    internal class Host
    {
        public Action<ITelegramBotClient, Update>? OnMessage;
        public Action<ITelegramBotClient, Update>? OnStartExam;

        TelegramBotClient bot;

        public Host(string token) 
        {
            bot = new TelegramBotClient(token); 
        }

        public void Start() 
        {
            bot.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("Бот запущен.");
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine($"Пришло сообщение: {update.Message?.Text ?? "<не текст>"}");
            OnMessage?.Invoke(client, update);
            OnStartExam?.Invoke(client, update);
            await Task.CompletedTask;
        }
    }
}
