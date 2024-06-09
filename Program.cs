using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Diagnostics;
using System.ComponentModel;
using Newtonsoft.Json.Converters;

namespace TelegramPDDBot
{
    internal class Program
    {
        //нужно сделать json файл с этими атрибутами
        static int questionStartCount = 4;

        static Dictionary<long, int> questionLimit = new Dictionary<long, int>();
        static Dictionary<long, int> questionCount = new Dictionary<long, int>();
        static Dictionary<long, int> rightCount = new Dictionary<long, int>();
        static Dictionary<long, int> mistakesCount = new Dictionary<long, int>();
        static Dictionary<long, bool> isExtended = new Dictionary<long, bool>(); //показывает, были ли добавлены вопросы     
        static Dictionary<long, List<JToken>> usedQuestions = new Dictionary<long, List<JToken>>();

        static bool isInformed = false;
        static bool isConverted = false;
        static int ticket = 0;
        static bool isTicketGiven = false;

        static Host bot = new Host("6997948675:AAFnP583_6yGO3CtRnWbYWawE5vw_NQvOzs"); // инициализация бота
        static Random rnd = new Random();
        static string category = "A_B";
        static void Main(string[] args)
        {
            bot.Start(); // запуск бота
            bot.OnMessage += OnMessage; // подписка на событие прихода сообщения
            Console.ReadLine();
        }

        static async Task EndMode(ITelegramBotClient client, Update update)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;
            bot.OnStartExam.Remove(chatId);
            questionCount.Remove(chatId);
            questionLimit.Remove(chatId);
            isExtended.Remove(chatId);
            rightCount.Remove(chatId);
            mistakesCount.Remove(chatId);
            usedQuestions[chatId].Remove(chatId);
            await ShowMenu(client, update);
        }

        static async Task CheckAnswer(ITelegramBotClient client, Update update)
        {
            switch (update.Type) //обработка кнопок
            {
                case UpdateType.CallbackQuery:
                    switch (update.CallbackQuery?.Data)
                    {
                        case "True":
                            await Console.Out.WriteLineAsync($"{questionCount[update.CallbackQuery.Message.Chat.Id]}) Ответ правильный");
                            rightCount[update.CallbackQuery.Message.Chat.Id]++;
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            break;
                        case "False":
                            await Console.Out.WriteLineAsync($"{questionCount[update.CallbackQuery.Message.Chat.Id]}) Ответ неправильный");
                            mistakesCount[update.CallbackQuery.Message.Chat.Id]++;
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            break;
                        case "Back":
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Работа режима завершена. Выход в меню...");
                            await EndMode(client, update);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        static JToken ChooseQuestion(Update update)
        {
            bool isUsed = true;
            usedQuestions[update.CallbackQuery.Message.Chat.Id] = new List<JToken>();
            JToken? question = default;
            while (isUsed) 
            {
                string ticketPath = $@"../../../pddQuestions/questions/{category}/tickets/Билет {rnd.Next(1, 41)}.json";
                string ticket = System.IO.File.ReadAllText(ticketPath);
                JArray jsonArray = JArray.Parse(ticket);
                question = jsonArray[rnd.Next(0, 20)];
                if (!usedQuestions[update.CallbackQuery.Message.Chat.Id].Contains(question))
                {
                    isUsed = false;
                }
            }
            usedQuestions[update.CallbackQuery.Message.Chat.Id].Add(question);
            return question;
        }

        static Stream GetImagePath(JToken question)
        {
            string imagePath = question["image"].ToString();
            imagePath = imagePath.Substring(1);
            imagePath = $@"../../../pddQuestions/{imagePath}";

            return System.IO.File.OpenRead(imagePath);
        }

        static string ParseAnswers(JArray answers)
        {
            string allAnswers = "";
            int j = 1;
            foreach (var answer in answers)
            {
                allAnswers += $"{j}) {answer["answer_text"]}\n";
                j++;
            }

            return allAnswers;
        }

        static async Task ShowMenu(ITelegramBotClient client, Update update) 
        {
            string checkTypeForName = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message?.Chat.FirstName : update.Message?.Chat.FirstName;
            long checkTypeForChat = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            await using Stream logo = System.IO.File.OpenRead(@"../../../pddQuestions/images/logo.jpg");
            await client.SendPhotoAsync(checkTypeForChat,
                photo: InputFile.FromStream(logo, "logo.jpg"),
                caption: $"{checkTypeForName}, Добро пожаловать в бота для изучения ПДД." +
                "\r\n\r\nСейчас вы находитесь на категории AВ (легковые автомобили и мотоциклы). Выберите режим работы:",
                replyMarkup: GetMenuButtons());
        }


        static async void StartExam(ITelegramBotClient client, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery) 
            {
                long chatId = update.CallbackQuery.Message.Chat.Id;

                if (!questionCount.ContainsKey(chatId))
                {
                    rightCount[chatId] = 0;
                    mistakesCount[chatId] = 0;
                    questionCount[chatId] = 0;
                    questionLimit[chatId] = questionStartCount;
                    isExtended[chatId] = false;
                    await client.DeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId);
                }
                if (update.Message?.Text != null)
                {
                    await client.SendTextMessageAsync(update.Message.Chat.Id, "В данном режиме принимаются ответы только на кнопки!");
                }
                else
                {
                    await Console.Out.WriteLineAsync($"{usedQuestions.Count}");

                    // обработка кнопок
                    await CheckAnswer(client, update);
                    if (questionLimit.ContainsKey(chatId))
                    {                       
                        if (questionCount[chatId] == questionLimit[chatId])
                        {
                            if (mistakesCount[chatId] > 0 && mistakesCount[chatId] <= 2 && !isExtended[chatId])
                            {
                                questionLimit[chatId] += 5 * mistakesCount[chatId];
                                isExtended[chatId] = true;
                                await client.SendTextMessageAsync(chatId, $"Ошибок было допущено: {mistakesCount[chatId]}. К экзамену добавлено {5 * mistakesCount[chatId]} вопросов.");
                            }
                        }

                        if (questionCount[chatId] != questionLimit[chatId])
                        {
                            //парсинг случайный выбор вопросов из json-формата 
                            JToken question = ChooseQuestion(update);

                            // получение пути к изображению
                            await using Stream image = GetImagePath(question);

                            //парсинг ответов 
                            JArray answers = JArray.Parse(question["answers"].ToString());
                            string allAnswers = ParseAnswers(answers);

                            //вывод вопросов
                            await client.SendPhotoAsync(chatId, photo: InputFile.FromStream(image, "question.jpg"),
                                caption: $"{question["question"]}\n\n{allAnswers}", replyMarkup: GetQuestionButtons(answers));
                           
                            questionCount[chatId]++;
                        }
                        else
                        {
                            string isPassed = mistakesCount[chatId] <= 2 ? "сдан" : "не сдан";
                            await client.SendTextMessageAsync(chatId, text: $"Экзамен {isPassed}! Количество правильных ответов = {rightCount[chatId]}/{questionLimit[chatId]}.");
                            await EndMode(client, update);
                        }
                    }                   
                }
            }           
        }

        static async void StartTickets(ITelegramBotClient client, Update update) 
        {
            if (!isInformed)
            {
                await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Введите номер билета (1 - 40)");
                isInformed = true;
            }
            if (!isTicketGiven)
            {
                if (update.Message?.Text != null)
                {
                    if (!isConverted)
                    {
                        isConverted = int.TryParse(update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Text : update.Message.Text, out ticket);
                    }
                    if (isConverted && ticket > 0 && ticket < 41)
                    {
                        isTicketGiven = true;
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id, "Введён неправильный формат номера билета. Попробуйте ввести номер билета ещё раз (1 - 40)");
                        isConverted = false;
                    }
                }
            }
            if (isTicketGiven)
            {
                //здесь должен быть основной код
                Console.WriteLine($"{ticket}");
            }
            
           
        }

        private static async void OnMessage(ITelegramBotClient client, Update update) //обработчик события
        {
            var message = update.Message;
            if (message?.Text == "/start") //вывод меню
            {
                //await using Stream logo = System.IO.File.OpenRead(@"../../../pddQuestions/images/logo.jpg");
                //await client.SendPhotoAsync(message.Chat.Id, photo: InputFile.FromStream(logo, "logo.jpg"), caption: $"{message.Chat.FirstName}, Добро пожаловать в бота для изучения ПДД." +
                //    "\r\n\r\nСейчас вы находитесь на категории AВ (легковые автомобили и мотоциклы). Выберите режим работы:",
                //    replyMarkup: GetMenuButtons());
                await ShowMenu(client, update);
            }

            switch (update.Type) //обработка кнопок
            {
                case UpdateType.CallbackQuery:
                    switch (update.CallbackQuery?.Data) 
                    {
                        case "Exam":
                            await Console.Out.WriteLineAsync("Режим: экзамен");
                            bot.OnStartExam[update.CallbackQuery.Message.Chat.Id] = default;
                            bot.OnStartExam[update.CallbackQuery.Message.Chat.Id] += StartExam;
                            break;
                        case "Tickets":
                            await Console.Out.WriteLineAsync("Режим: билеты");
                            bot.OnStartTickets += StartTickets;
                            break;
                        case "Topics":
                            await Console.Out.WriteLineAsync("Режим: вопросы по темам");
                            break;
                        case "Mistakes":
                            await Console.Out.WriteLineAsync("Режим: ошибки");
                            break;
                        case "Category":
                            await Console.Out.WriteLineAsync("Режим: категории");
                            break;
                        case "Help":
                            await Console.Out.WriteLineAsync("Режим: помощь");
                            break;
                    }
                    break;
            }
        }

        private static IReplyMarkup? GetQuestionButtons(JToken answers)
        {
            List<InlineKeyboardButton> buttons= new List<InlineKeyboardButton>();
            List<string> nums = new List<string>() { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣" };

            int i = 0;
            foreach (var answer in answers)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData(text: nums[i], callbackData: $"{answer["is_correct"]}"));
                i++;
            }
            return new InlineKeyboardMarkup
            (
                new List<List<InlineKeyboardButton>>() 
                {
                    buttons,
                    new List<InlineKeyboardButton>() 
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Вернуться в меню", callbackData: "Back")
                    }
                }             
            );
        }

        private static IReplyMarkup? GetMenuButtons()
        {
            return new InlineKeyboardMarkup
            (
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Экзамен", callbackData: "Exam")
                    },
                    new[]
                    {
                         InlineKeyboardButton.WithCallbackData(text: "Билеты", callbackData: "Tickets")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Вопросы по темам", callbackData: "Topics")
                    },
                    new[]
                    {
                         InlineKeyboardButton.WithCallbackData(text: "Ошибки", callbackData: "Mistakes")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Сменить категорию", callbackData: "Category")
                    },
                    new[]
                    {
                         InlineKeyboardButton.WithCallbackData(text: "Руководство пользователя", callbackData: "Help")
                    },
                }          
            );
        }
    }
}
