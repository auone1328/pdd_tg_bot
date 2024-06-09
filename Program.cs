﻿using Telegram.Bot;
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
        static int questionStartCount = 20;
        static Dictionary<long, int> questionLimit = new Dictionary<long, int>();
        static int questionCount = 0;
        static int rightCount = 0;
        static int mistakesCount = 0;
        static bool isExtended = false; //показывает, были ли добавлены вопросы     
        static List<JToken> usedQuestions = new List<JToken>();

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

        static async Task CheckAnswer(ITelegramBotClient client, Update update)
        {
            switch (update.Type) //обработка кнопок
            {
                case UpdateType.CallbackQuery:
                    switch (update.CallbackQuery?.Data)
                    {
                        case "True":
                            await Console.Out.WriteLineAsync($"{questionCount}) Ответ правильный");
                            rightCount++;
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            break;
                        case "False":
                            await Console.Out.WriteLineAsync($"{questionCount}) Ответ неправильный");
                            mistakesCount++;
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        static JToken ChooseQuestion()
        {
            bool isUsed = true;
            JToken? question = default;
            while (isUsed) 
            {
                string ticketPath = $@"../../../pddQuestions/questions/{category}/tickets/Билет {rnd.Next(1, 41)}.json";
                string ticket = System.IO.File.ReadAllText(ticketPath);
                JArray jsonArray = JArray.Parse(ticket);
                question = jsonArray[rnd.Next(0, 20)];
                if (!usedQuestions.Contains(question)) 
                {
                    isUsed = false;
                }
            }
            usedQuestions.Add(question);
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
            string textForMenu;
            if (category == "A_B")
                textForMenu = "(легковые автомобили и мотоциклы)";
            else
                textForMenu = "(грузовые автомобили и автобусы)";
            string checkTypeForName = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message?.Chat.FirstName : update.Message?.Chat.FirstName;
            long checkTypeForChat = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            await using Stream logo = System.IO.File.OpenRead(@"../../../pddQuestions/images/logo.jpg");
            await client.SendPhotoAsync(checkTypeForChat,
                photo: InputFile.FromStream(logo, "logo.jpg"),
                caption: $"{checkTypeForName}, Добро пожаловать в бота для изучения ПДД." +
                $"\r\n\r\nСейчас вы находитесь на категории {category} {textForMenu}. Выберите режим работы:",
                replyMarkup: GetMenuButtons());
        }

        private static InlineKeyboardMarkup? GetCategoryButtons()
        {
            return new InlineKeyboardMarkup
            (
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "A_B", callbackData: "A_B")
                    },
                    new[]
                    {
                         InlineKeyboardButton.WithCallbackData(text: "C_D", callbackData: "C_D")
                    },
                }
            );
        }

        static async void ChangeCategory(ITelegramBotClient client, Update update)
        {
            if (update.Message?.Text != null)
            {
                await client.SendTextMessageAsync(update.Message.Chat.Id, "В данном режиме принимаются ответы только на кнопки!");
            }
            else if (update.CallbackQuery?.Data != null)
            {
                string selectedCategory = update.CallbackQuery.Data;
                if (selectedCategory == "A_B" || selectedCategory == "C_D")
                {
                    category = selectedCategory;
                    await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                    bot.OnChangeCategory -= ChangeCategory;
                    await ShowMenu(client, update);
                }
                else
                {
                    await client.SendTextMessageAsync(
                        update.CallbackQuery.Message.Chat.Id,
                        text: "Выберите категорию:",
                        replyMarkup: GetCategoryButtons());
                }
            }
        }


        static async void StartExam(ITelegramBotClient client, Update update)
        {
            if (questionCount == 0)
            {
                questionLimit[update.CallbackQuery.Message.Chat.Id] = questionStartCount;
                await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
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

                if (questionCount == questionLimit[update.CallbackQuery.Message.Chat.Id])
                {
                    if (mistakesCount > 0 && mistakesCount <= 2 && !isExtended)
                    {
                        questionLimit[update.CallbackQuery.Message.Chat.Id] += 5 * mistakesCount;
                        isExtended = true;
                        await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибок было допущено: {mistakesCount}. К экзамену добавлено {5 * mistakesCount} вопросов.");
                    }
                }

                if (questionCount != questionLimit[update.CallbackQuery.Message.Chat.Id])
                {
                    //парсинг случайный выбор вопросов из json-формата 
                    JToken question = ChooseQuestion();

                    // получение пути к изображению
                    await using Stream image = GetImagePath(question);

                    //парсинг ответов 
                    JArray answers = JArray.Parse(question["answers"].ToString());
                    string allAnswers = ParseAnswers(answers);

                    //вывод вопросов
                    await client.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, photo: InputFile.FromStream(image, "question.jpg"),
                        caption: $"{question["question"]}\n\n{allAnswers}", replyMarkup: GetQuestionButtons(answers));

                    questionCount++;

                }
                else
                {
                    bot.OnStartExam -= StartExam;
                    string isPassed = mistakesCount <= 2 ? "сдан" : "не сдан";
                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, text: $"Экзамен {isPassed}! Количество правильных ответов = {rightCount}/{questionLimit[update.CallbackQuery.Message.Chat.Id]}.");
                    questionCount = 0;
                    questionLimit.Remove(update.CallbackQuery.Message.Chat.Id);
                    isExtended = false;
                    rightCount = 0;
                    mistakesCount = 0;
                    await ShowMenu(client, update);
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
                            bot.OnStartExam += StartExam;
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
                            bot.OnChangeCategory += ChangeCategory;
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
                buttons
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
