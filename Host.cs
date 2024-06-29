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
        //public Action<ITelegramBotClient, Update>? OnStartTickets;
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnStartExam = new Dictionary<long, Action<ITelegramBotClient, Update>?>();
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnChangeCategory = new Dictionary<long, Action<ITelegramBotClient, Update>?>();
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnStartTickets = new Dictionary<long, Action<ITelegramBotClient, Update>?>();
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnStartMistakes = new Dictionary<long, Action<ITelegramBotClient, Update>?>();
        public Dictionary<long, Action<ITelegramBotClient, Update>?> OnUserGuide = new Dictionary<long, Action<ITelegramBotClient, Update>?>();

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

        delegate void Mode(ITelegramBotClient client, Update update);

        static void StartMode(ITelegramBotClient client, Update update, Dictionary<long, Action<ITelegramBotClient, Update>?> action, Mode mode)
        {
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            if (action.ContainsKey(chatId))
            {
                mode(client, update);
            }
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine($"Пришло сообщение: {update.Message?.Text ?? "<не текст>"}");
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;

            OnMessage?.Invoke(client, update);
            StartMode(client, update, OnStartExam, (client, update) => OnStartExam[chatId]?.Invoke(client, update));
            StartMode(client, update, OnChangeCategory, (client, update) => OnChangeCategory[chatId]?.Invoke(client, update));
            StartMode(client, update, OnStartTickets, (client, update) => OnStartTickets[chatId]?.Invoke(client, update));
            StartMode(client, update, OnStartMistakes, (client, update) => OnStartMistakes[chatId]?.Invoke(client, update));
            StartMode(client, update, OnUserGuide, (client, update) => OnUserGuide[chatId]?.Invoke(client, update));

            await Task.CompletedTask;
        }
    }
}
