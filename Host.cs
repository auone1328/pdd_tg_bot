using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramPDDBot
{
    internal class Host
    {
        public Action<ITelegramBotClient, Update>? OnMessage;
        //public Action<ITelegramBotClient, Update>? OnStartExam;
        public Action<ITelegramBotClient, Update>? OnStartTickets;
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnStartExam = new Dictionary<long, Action<ITelegramBotClient, Update>?>(); 

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
            Console.WriteLine($"Ошибка: {exception}");
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine($"Пришло сообщение: {update.Message?.Text ?? "<не текст>"}");
            OnMessage?.Invoke(client, update);
            if (update.Type == UpdateType.CallbackQuery && OnStartExam.ContainsKey(update.CallbackQuery.Message.Chat.Id))
            {
                OnStartExam[update.CallbackQuery.Message.Chat.Id]?.Invoke(client, update);
            }
            OnStartTickets?.Invoke(client, update);
            OnChangeCategory?.Invoke(client, update);
            await Task.CompletedTask;
        }
    }
}
