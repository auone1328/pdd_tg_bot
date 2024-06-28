using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace TelegramPDDBot
{
    internal class Program
    {
        static Dictionary<long, bool> isInMode = new Dictionary<long, bool>();

        //for exam
        static int questionStartCount = 20;
        static Dictionary<long, int> questionLimit = new Dictionary<long, int>();
        static Dictionary<long, int> questionCount = new Dictionary<long, int>();
        static Dictionary<long, int> rightCount = new Dictionary<long, int>();
        static Dictionary<long, int> mistakesCount = new Dictionary<long, int>();
        static Dictionary<long, bool> isExtended = new Dictionary<long, bool>(); //показывает, были ли добавлены вопросы     
        static Dictionary<long, List<string>> usedQuestions = new Dictionary<long, List<string>>();

        //for tickets
        static Dictionary<long, bool> isInformed = new Dictionary<long, bool>();
        static Dictionary<long, bool> isConverted = new Dictionary<long, bool>();
        static Dictionary<long, int> ticket = new Dictionary<long, int>();
        static Dictionary<long, bool> isTicketGiven = new Dictionary<long, bool>();
        static Dictionary<long, JToken> currentQuestion = new Dictionary<long, JToken>();
 
        static Host bot = new Host("6997948675:AAFnP583_6yGO3CtRnWbYWawE5vw_NQvOzs"); // инициализация бота
        static Random rnd = new Random();

        static string startCategory = "A_B";
        static Dictionary<long, string> category = new Dictionary<long, string>();

        static void Main(string[] args)
        {
            bot.Start(); // запуск бота
            bot.OnMessage += OnMessage; // подписка на событие прихода сообщения
            Console.ReadLine();
        }

        static async Task EndMode(ITelegramBotClient client, Update update, Dictionary<long, Action<ITelegramBotClient, Update>?> modeType)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;
            modeType.Remove(chatId);
            questionCount.Remove(chatId);
            questionLimit.Remove(chatId);
            isExtended.Remove(chatId);
            rightCount.Remove(chatId);
            mistakesCount.Remove(chatId);
            usedQuestions.Remove(chatId);
            isInMode.Remove(chatId);
            currentQuestion.Remove(chatId);
            await ShowMenu(client, update);
        }

        static JArray ReadMistakesFile(Update update) 
        {       
            string mistakesPath = $@"../../../pddQuestions/mistakes/{update.CallbackQuery.Message.Chat.Id}.json";
            string mistakes = System.IO.File.ReadAllText(mistakesPath);
            JArray? mistakesArray = null;

            try 
            {
                mistakesArray = JArray.Parse(mistakes);
            }
            catch 
            {
                mistakesArray = new JArray();
            }

            return mistakesArray;
        }

        static void AddQuestionToMistakes(Update update, JToken question, JArray answers) 
        {
            Question mistake = new Question(question["id"].ToString(), question["image"].ToString(),
                question["question"].ToString(), answers, question["answer_tip"].ToString(),
                question["correct_answer"].ToString());

            JToken mistakeToken = JToken.FromObject(mistake);
            JArray mistakesArray;
            try
            {
                mistakesArray = ReadMistakesFile(update);
            }
            catch 
            {
                mistakesArray = new JArray();
            }

            int isContains = mistakesArray.Where(item => item.ToString() == mistakeToken.ToString()).Count();

            if (isContains == 0) 
            {
                mistakesArray.Add(mistakeToken);
                System.IO.File.WriteAllText($@"../../../pddQuestions/mistakes/{update.CallbackQuery.Message.Chat.Id}.json", JsonConvert.SerializeObject(mistakesArray));
            }            
        }


        static void EditMistakes(Update update, JToken question) 
        {
            JArray jsonArray = ReadMistakesFile(update);
            jsonArray.Where(item => item["QuestionId"].ToString() == question["QuestionId"].ToString()).ToList().ForEach(item => item.Remove());
            System.IO.File.WriteAllText($@"../../../pddQuestions/mistakes/{update.CallbackQuery.Message.Chat.Id}.json", JsonConvert.SerializeObject(jsonArray));
        }

        static async Task CheckAnswerExam(ITelegramBotClient client, Update update, JToken question, JArray answers)
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

                            AddQuestionToMistakes(update, question, answers);
                            break;
                        case "Back":
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Работа режима завершена. Выход в меню...");
                            await EndMode(client, update, bot.OnStartExam);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }



        static async Task CheckAnswerQuestion(ITelegramBotClient client, Update update, JToken question, Stream image, JArray answers, string allAnswers, bool isMistakes)
        {

            switch (update.Type) //обработка кнопок
            {                
                case UpdateType.CallbackQuery:
                    switch (update.CallbackQuery.Data)
                    {
                        case "True":
                            await Console.Out.WriteLineAsync($"{questionCount[update.CallbackQuery.Message.Chat.Id]}) Ответ правильный");                          
                            rightCount[update.CallbackQuery.Message.Chat.Id]++;
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{questionCount[update.CallbackQuery.Message.Chat.Id]}) ✅Ответ верный.");
    
                            if (isMistakes)
                            {
                                EditMistakes(update, question);
                            }
                            break;
                        case "False":
                            await Console.Out.WriteLineAsync($"{questionCount[update.CallbackQuery.Message.Chat.Id]}) Ответ неправильный");
                            mistakesCount[update.CallbackQuery.Message.Chat.Id]++;                           
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);

                            string questionText = isMistakes ? "GivenQuestion" : "question";
                            string correctAnswer = isMistakes ? "CorrectAnswer" : "correct_answer";
                            string answerTip = isMistakes ? "AnswerTip" : "answer_tip";
                          

                            try
                            {
                                await client.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, photo: InputFile.FromStream(image, "question.jpg"),
                                caption: $"{question[questionText]}\n{allAnswers}\n{questionCount[update.CallbackQuery.Message.Chat.Id]}) ❌Ответ неверный.\n{question[correctAnswer]}\n{question[answerTip]}");
                            }   
                            catch 
                            {
                                await using Stream photoToInsert = isMistakes ? GetImagePathForMistakes(question) : GetImagePath(question);
                                await client.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, photo: InputFile.FromStream(photoToInsert, "question.jpg"),
                                caption: $"{question[questionText]}\n{allAnswers}\n{questionCount[update.CallbackQuery.Message.Chat.Id]}) ❌Ответ неверный.\n{question[correctAnswer]}");
                            }

                             if (!isMistakes)
                            {
                                AddQuestionToMistakes(update, question, answers);
                            }
                            break;
                        case "Back":
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Работа режима завершена. Выход в меню...");
                            if (isMistakes)
                            {
                                await EndMode(client, update, bot.OnStartMistakes);
                            }
                            else 
                            {
                                await EndMode(client, update, bot.OnStartTickets);
                            }
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
            long chatId = update.CallbackQuery.Message.Chat.Id;
            usedQuestions[chatId] = new List<string>();
            JToken? question = default;
            while (isUsed) 
            {
                string ticketPath = $@"../../../pddQuestions/questions/{category[chatId]}/tickets/Билет {rnd.Next(1, 41)}.json";
                string ticket = System.IO.File.ReadAllText(ticketPath);
                JArray jsonArray = JArray.Parse(ticket);
                question = jsonArray[rnd.Next(0, 20)];
                if (!usedQuestions[chatId].Contains(question.ToString()))
                {
                    isUsed = false;
                }
            }
            usedQuestions[chatId].Add(question.ToString());
            return question;
        }

        static JToken ChooseQuestion(Update update, int ticketNumber)
        {
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;

            if (!usedQuestions.ContainsKey(chatId)) 
            {
                usedQuestions[chatId] = new List<string>();
            }
     
            JToken? question = default;
            string ticketPath = $@"../../../pddQuestions/questions/{category[chatId]}/tickets/Билет {ticketNumber}.json";
            string ticket = System.IO.File.ReadAllText(ticketPath);
            JArray jsonArray = JArray.Parse(ticket);
            for (int i = 0; i < jsonArray.Count; i++) 
            {              
                if (!usedQuestions[chatId].Contains(jsonArray[i].ToString())) 
                {
                    question = jsonArray[i];                  
                    usedQuestions[chatId].Add(question.ToString());
                    break;
                }               
            }

            return question;
        }

        static int CountMistakeQuestions(Update update)
        {
            try 
            {
                string mistakesPath = $@"../../../pddQuestions/mistakes/{update.CallbackQuery.Message.Chat.Id}.json";
                string mistakes = System.IO.File.ReadAllText(mistakesPath);
                JArray jsonArray = JArray.Parse(mistakes);
                int count = jsonArray.Count;

                return count;
            }
            catch 
            {
                return 0;
            }
        }

        static JToken ChooseQuestionForMistakes(Update update)
        {
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;

            if (!usedQuestions.ContainsKey(chatId))
            {
                usedQuestions[chatId] = new List<string>();
            }

            JToken? question = default;
            string mistakesPath = $@"../../../pddQuestions/mistakes/{chatId}.json";
            string mistakes = System.IO.File.ReadAllText(mistakesPath);
            JArray jsonArray = JArray.Parse(mistakes);
            for (int i = 0; i < jsonArray.Count; i++)
            {
                if (!usedQuestions[chatId].Contains(jsonArray[i].ToString()))
                {
                    question = jsonArray[i];
                    usedQuestions[chatId].Add(question.ToString());
                    break;
                }
            }

            return question;
        }

        static Stream GetImagePath(JToken question)
        {
            string imagePath = question["image"].ToString();
            imagePath = imagePath.Substring(1);
            imagePath = $@"../../../pddQuestions/{imagePath}";

            return System.IO.File.OpenRead(imagePath);
        }

        static Stream GetImagePathForMistakes(JToken question)
        {
            string imagePath = question["ImagePath"].ToString();
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
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;

            if (category[chatId] == "A_B")
                textForMenu = "(легковые автомобили и мотоциклы)";
            else
                textForMenu = "(грузовые автомобили и автобусы)";

            string checkTypeForName = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message?.Chat.FirstName : update.Message?.Chat.FirstName;
            long checkTypeForChat = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;

            await using Stream logo = System.IO.File.OpenRead(@"../../../pddQuestions/images/logo.jpg");
            await client.SendPhotoAsync(checkTypeForChat,
                photo: InputFile.FromStream(logo, "logo.jpg"),
                caption: $"{checkTypeForName}, Добро пожаловать в бота для изучения ПДД." +
                $"\r\n\r\nСейчас вы находитесь на категории {category[chatId]} {textForMenu}. Выберите режим работы:",
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
                    category[update.CallbackQuery.Message.Chat.Id] = selectedCategory;
                    await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                    bot.OnChangeCategory[update.CallbackQuery.Message.Chat.Id] -= ChangeCategory;
                    isInMode.Remove(update.CallbackQuery.Message.Chat.Id);
                    await ShowMenu(client, update);
                }
                else
                {
                    await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                    await client.SendTextMessageAsync(
                        update.CallbackQuery.Message.Chat.Id,
                        text: "Выберите категорию:",
                        replyMarkup: GetCategoryButtons());
                }
            }
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

                await Console.Out.WriteLineAsync($"{usedQuestions.Count}");

                // обработка кнопок
                if (currentQuestion.ContainsKey(chatId))
                {
                    JArray answers = JArray.Parse(currentQuestion[chatId]["answers"].ToString());
                    await CheckAnswerExam(client, update, currentQuestion[chatId], answers);
                }

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
                        currentQuestion[chatId] = question;
                        // получение пути к изображению
                        await using Stream image = GetImagePath(question);

                        //парсинг ответов 
                        JArray answers = JArray.Parse(question["answers"].ToString());
                        string allAnswers = ParseAnswers(answers);

                        //вывод вопросов
                        await client.SendPhotoAsync(chatId, photo: InputFile.FromStream(image, "question.jpg"),
                            caption: $"{questionCount[chatId] + 1}) {question["question"]}\n\n{allAnswers}", replyMarkup: GetQuestionButtons(answers));

                        questionCount[chatId]++;
                    }
                    else
                    {
                        string isPassed = mistakesCount[chatId] <= 2 ? "сдан✅" : "не сдан❌";
                        await client.SendTextMessageAsync(chatId, text: $"Экзамен {isPassed}! Количество правильных ответов = {rightCount[chatId]}/{questionLimit[chatId]}.");
                        await EndMode(client, update, bot.OnStartExam);
                    }
                }
            }           
        }

        static async void StartTickets(ITelegramBotClient client, Update update)
        {
            //запрос номера билета 
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            if (!isInformed[chatId])
            {
                await client.DeleteMessageAsync(chatId, update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.MessageId : update.Message.MessageId);
                await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Введите номер билета (1 - 40)");
                isInformed[chatId] = true;
            }
            if (!isTicketGiven[chatId])
            {
                if (update.Message?.Text != null)
                {
                    if (!isConverted[chatId])
                    {
                        int inputTicket;
                        isConverted[chatId] = int.TryParse(update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Text : update.Message.Text, out inputTicket);
                        ticket[chatId] = inputTicket;
                    }
                    if (isConverted[chatId] && ticket[chatId] > 0 && ticket[chatId] < 41)
                    {
                        isTicketGiven[chatId] = true;
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id, "Введён неправильный формат номера билета. Попробуйте ввести номер билета ещё раз (1 - 40)");
                        isConverted[chatId] = false;
                    }
                }
            }
            if (isTicketGiven[chatId])
            {
                //включение режима
                Console.WriteLine($"{ticket[chatId]}");
                if (update.Type == UpdateType.CallbackQuery || !questionCount.ContainsKey(chatId))
                {
                    if (!questionCount.ContainsKey(chatId))
                    {
                        rightCount[chatId] = 0;
                        mistakesCount[chatId] = 0;
                        questionCount[chatId] = 0;
                        questionLimit[chatId] = questionStartCount;
                        isExtended[chatId] = false;
                    }

                    await Console.Out.WriteLineAsync($"{usedQuestions.Count}");


                    // обработка кнопок
                    if (currentQuestion.ContainsKey(chatId)) 
                    {
                        JArray answers = JArray.Parse(currentQuestion[chatId]["answers"].ToString());
                        await CheckAnswerQuestion(client, update, currentQuestion[chatId], GetImagePath(currentQuestion[chatId]), answers, ParseAnswers(answers), false);
                    }                           
                    
                    if (questionLimit.ContainsKey(chatId))
                    {
                        if (questionCount[chatId] != questionLimit[chatId])
                        {
                            //парсинг вопроса из json-формата 
                            JToken question = ChooseQuestion(update, ticket[chatId]);
                            currentQuestion[chatId] = question;
                            // получение пути к изображению
                            await using Stream image = GetImagePath(question);
                            //парсинг ответов 
                            JArray answers = JArray.Parse(question["answers"].ToString());
                            string allAnswers = ParseAnswers(answers);
                            //вывод вопросов
                            await client.SendPhotoAsync(chatId, photo: InputFile.FromStream(image, "question.jpg"),
                                caption: $"{questionCount[chatId] + 1}) {question["question"]}\n\n{allAnswers}", replyMarkup: GetQuestionButtons(answers));

                            questionCount[chatId]++;
                        }
                        else
                        {
                            string isPassed = mistakesCount[chatId] <= 2 ? "пройденным✅" : "непройденным❌";
                            await client.SendTextMessageAsync(chatId, text: $"Билет считается {isPassed}. Количество правильных ответов = {rightCount[chatId]}/{questionLimit[chatId]}.");
                            await EndMode(client, update, bot.OnStartTickets);
                        }                       
                    }
                }
            }
        }


        static async void StartMistakes(ITelegramBotClient client, Update update)
        {
            long chatId = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            //включение режима
            if (update.Type == UpdateType.CallbackQuery || !questionCount.ContainsKey(chatId))
            {
                if (!questionCount.ContainsKey(chatId))
                {
                    rightCount[chatId] = 0;
                    mistakesCount[chatId] = 0;
                    questionCount[chatId] = 0;
                    questionLimit[chatId] = CountMistakeQuestions(update);
                    isExtended[chatId] = false;
                    await client.DeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId);
                }

                await Console.Out.WriteLineAsync($"{usedQuestions.Count}");

                // обработка кнопок
                if (currentQuestion.ContainsKey(chatId))
                {
                    JArray answers = JArray.Parse(currentQuestion[chatId]["Answers"].ToString());
                    await CheckAnswerQuestion(client, update, currentQuestion[chatId], GetImagePathForMistakes(currentQuestion[chatId]), answers, ParseAnswers(answers).ToLower(), true);
                }

                if (questionLimit.ContainsKey(chatId))
                {
                    if (questionCount[chatId] != questionLimit[chatId])
                    {
                        //парсинг вопроса из json-формата 
                        JToken question = ChooseQuestionForMistakes(update);
                        currentQuestion[chatId] = question;
                        // получение пути к изображению
                        await using Stream image = GetImagePathForMistakes(question);
                        //парсинг ответов 
                        JArray answers = JArray.Parse(question["Answers"].ToString());
                        string allAnswers = ParseAnswers(answers);
                        //вывод вопросов
                        await client.SendPhotoAsync(chatId, photo: InputFile.FromStream(image, "question.jpg"),
                            caption: $"{questionCount[chatId] + 1}) {question["GivenQuestion"]}\n\n{allAnswers}", replyMarkup: GetQuestionButtons(answers));

                        questionCount[chatId]++;
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, text: "Ошибки закончились.");
                        await EndMode(client, update, bot.OnStartMistakes);
                    }
                }
            }

        }


        private static async void OnMessage(ITelegramBotClient client, Update update) //обработчик события
        {
            var message = update.Type == UpdateType.CallbackQuery ? update.CallbackQuery.Message : update.Message; ;
            if (!isInMode.ContainsKey(message.Chat.Id)) 
            {
                isInMode[message.Chat.Id] = false;
            }
            if (message?.Text == "/start" && !isInMode[message.Chat.Id]) //вывод меню
            {
                //await using Stream logo = System.IO.File.OpenRead(@"../../../pddQuestions/images/logo.jpg");
                //await client.SendPhotoAsync(message.Chat.Id, photo: InputFile.FromStream(logo, "logo.jpg"), caption: $"{message.Chat.FirstName}, Добро пожаловать в бота для изучения ПДД." +
                //    "\r\n\r\nСейчас вы находитесь на категории AВ (легковые автомобили и мотоциклы). Выберите режим работы:",
                //    replyMarkup: GetMenuButtons());
                category[message.Chat.Id] = startCategory;
                await ShowMenu(client, update);
            }

            switch (update.Type) //обработка кнопок
            {
                case UpdateType.CallbackQuery:
                    if (!category.ContainsKey(message.Chat.Id)) 
                    {
                        category[message.Chat.Id] = startCategory;
                    }
                    switch (update.CallbackQuery?.Data) 
                    {
                        case "Exam":
                            await Console.Out.WriteLineAsync("Режим: экзамен");
                            bot.OnStartExam[update.CallbackQuery.Message.Chat.Id] = default;
                            bot.OnStartExam[update.CallbackQuery.Message.Chat.Id] += StartExam;
                            isInMode[update.CallbackQuery.Message.Chat.Id] = true; 
                            break;
                        case "Tickets":
                            await Console.Out.WriteLineAsync("Режим: билеты");
                            isInformed[update.CallbackQuery.Message.Chat.Id] = false;
                            isConverted[update.CallbackQuery.Message.Chat.Id] = false;
                            ticket[update.CallbackQuery.Message.Chat.Id] = 0;
                            isTicketGiven[update.CallbackQuery.Message.Chat.Id] = false;

                            bot.OnStartTickets[update.CallbackQuery.Message.Chat.Id] = default;
                            bot.OnStartTickets[update.CallbackQuery.Message.Chat.Id] += StartTickets;
                            isInMode[update.CallbackQuery.Message.Chat.Id] = true;
                            break;
                        case "Mistakes":
                            await Console.Out.WriteLineAsync("Режим: ошибки");
                            bot.OnStartMistakes[update.CallbackQuery.Message.Chat.Id] = default;
                            bot.OnStartMistakes[update.CallbackQuery.Message.Chat.Id] += StartMistakes;
                            isInMode[update.CallbackQuery.Message.Chat.Id] = true;
                            break;
                        case "Category":
                            await Console.Out.WriteLineAsync("Режим: категории");
                            bot.OnChangeCategory[update.CallbackQuery.Message.Chat.Id] = default;
                            bot.OnChangeCategory[update.CallbackQuery.Message.Chat.Id] += ChangeCategory;
                            isInMode[update.CallbackQuery.Message.Chat.Id] = true;
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
