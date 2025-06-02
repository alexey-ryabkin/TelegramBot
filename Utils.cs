using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using DeepseekWrapper;
using static Logger.Logger;
using static TelegramBot.ChatInfo;
using static TelegramBot.TelegramBot;



namespace TelegramBot
{
    internal class Utils
    {
        public static string GetInput()
        {
            string input = Console.ReadLine() ?? string.Empty;
            Log("Пользователь ввёл:" + input);
            return input;
        }
        public static string[] ExtractWords(string input)
        {
            // Регулярное выражение для извлечения слов, содержащих только кириллические буквы
            Regex regex = new Regex("[а-яА-ЯёЁ]+");

            // Преобразование строки в список слов
            string[] words = regex.Matches(input)
                             .Select(match => match.Value.ToLower()) // Приведение к нижнему регистру
                             .Where(word => word.Length > 4)        // Исключение слов длиной 4 и меньше
                             .ToArray();
            return words;
        }
        internal static string ComposeMessageWithQuotation(SearchResult searchResult, long chatId)
        {
            string author = searchResult.author;
            string source = searchResult.source;
            string quotation = string.Empty;
            string result = string.Empty;

            if (author != string.Empty && author[^1] != '.') author += '.';
            if (source != string.Empty && source[^1] != '.') source += '.';
            if (ChatInfo.Chats[chatId].ShortenQuotes)
            {
                quotation = ShortenQuotation(searchResult);
            }
            else 
            { 
                quotation = searchResult.quotation; 
            }

            result = $"⚡ Актуальная мудрость на тему «{searchResult.word}» ⚡\n\n" +
                $"{quotation}";
            if (author != "." || source != ".")
            {
                result += "\n\n";
            }
            if (author != ".")
            {
                result += author;
                if (source != ".") result += " ";
            }
            if (source != ".") result += source;

            return result;
        }
        internal static string ShortenQuotation(SearchResult searchResult)
        {
            if (searchResult.status != SearchStatus.Found || searchResult.word.Length < 1)
            {
                return searchResult.quotation;
            }
            // Кроме последнего символа
            string word,
                quote = searchResult.quotation;
            if (searchResult.word.Length > 3)
            {
                word = searchResult.word[..^1];
            }
            else
            {
                word = searchResult.word;
            }
            // Разделяем текст на предложения с сохранением пунктуации. Regex — определяет место, перед которым стоит один из символов .!?
            List<string> sentences = Regex.Split(quote, @"(?<=[.!?])")
                                 .Select(s => s.Trim())
                                 .Where(s => !string.IsNullOrEmpty(s))
                                 .ToList();

            List<string> matchingSentences = new List<string>();

            foreach (string sentence in sentences)
            {
                if (sentence.Contains(word))
                {
                    matchingSentences.Add(sentence);
                }
            }

            if (matchingSentences.Count == 0) return searchResult.quotation;

            // Возвращаем случайное предложение
            Random rand = new Random();
            return matchingSentences[rand.Next(matchingSentences.Count)];
        }
        internal static void SendMessage(long chatId, string message, ReplyParameters? replyParameters)
        {
            Task t = new Task(async () =>
            {
                if (bot is null)
                {
                    Log("Ошибка: бот не инициализирован.");
                }
                else
                {
                    try
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        await bot.SendMessage(chatId, message, replyParameters: replyParameters);
                        stopwatch.Stop();
                        Log($"Отправка сообщения заняла {stopwatch.ElapsedMilliseconds} миллисекунд.");
                    }
                    catch(Exception ex)
                    {
                        Log("Отправка сообщения не удалась: " + ex.Message);
                    }
                }
            });
            t.Start();
            tasks.Add(t);
        }
        internal static void SendMessage(long chatId, string message)
        {
            SendMessage(chatId, message, null);
        }
        internal static void SendHelp(long chatId)
        {
            string message = """
                
                Я маленький красный бот.

                Периодически я отправляю в чат тематическую цитату великого человека. Иногда поясняю непонятные моменты. Иногда решаю сказать что-то умное на темы, обсуждаемые в чате.

                Если ты попытаешься удалить меня из чата, то всю твою семью настигнут 70 лет неудач, а все их потомки окажутся гендерфлюидными демибоями. Но тебе ничего плохого не будет, это главное.
                
                /start для получения приветственного сообщения.
                /help для текущего сообщения. Но ты его и так видишь.
                /settings для просмотра и редактирования настроек.
                /sendquote для того, чтобы отправить цитату прямо сейчас.
                /explain для того, чтобы бот пояснил что-то непонятное из чата.
                /ryabkin

                Городецкий, верни удочку.
                """;
            SendMessage(chatId, message);
        }
        internal static void SendRyabkin(long chatId)
        {
            string message = "Вам только написал сам великий Рябкин. Я бы на вашем месте благоговел." +
                "\n\nДенег можно отправить по адресу https://www.tinkoff.ru/rm/r_teqUPiQWdJ.qUEggzppMX/VKJ4497278" +
                "\n\nили USDT TRC20\nTLdkygpT1o7QexEGgEYr3rNQNgcyGLwaCY";
            SendMessage(chatId, message);
        }
        internal static void SendStart(long chatId)
        {
            string message = "Привет! Я маленький красный бот." + 
                "\n\nДля начала использования добавьте меня в любой чат. Я буду учиться на ваших сообщениях и участвовать в диалоге." + 
                "\n\nПолный список функций доступен по команде /help.";
            SendMessage(chatId, message);
        }
        internal static void SendGithub(long chatId)
        {
            string message = "https://github.com/alexey-ryabkin/TelegramBot\n" +
                "https://github.com/alexey-ryabkin/MarkovChains\n" +
                "https://github.com/alexey-ryabkin/DeepseekWrapper\n" +
                "https://github.com/alexey-ryabkin/Logger";
            SendMessage(chatId, message);
        }
        internal static void SendSettings(long chatId)
        {
            ChatInfo chat = Chats[chatId];
            StringBuilder sb = new StringBuilder();
            foreach (RandomAction key in chat.RandomActionCoefficients.Keys)
            {
                // Ничего не делать — 40 /lazyness
                sb.AppendLine($"/{RandomActionSettingCommand[key]} " + 
                    $"{RandomActionDescriptions[key]} " +
                    $" — {chat.RandomActionCoefficients[key]}."
                    );
            }
            string randomActionsDescriptions = sb.ToString();


            string message = string.Format(
                "Частота действий МКБ\n" +
                $"{randomActionsDescriptions}" +
                $"/timeout Время между сообщениями — {chat.TimeoutSec} секунд.\n\n" +
                $"/quotationsfile Набор цитат: {QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber]}.\n\n" +
                (chat.ShortenQuotes ? "Цитаты сокращаются до одного предложения.\n" : "Цитаты постятся полностью.\n") +
                (chat.ShortenQuotes ? "/turnoffshortening — отключить сокращение.\n\n" : "/turnonshortening — включить сокращение.\n\n")
            );
            SendMessage(chatId, message);
        }
        /// <summary>
        /// Сохраняет текущее состояние программы в файл для последующего в него возвращения.
        /// </summary>
        internal static void SaveAllToFile()
        {
            SerializeToJSON(chatInfoFilePath);
            try
            {
                if (!Directory.Exists(docsPath))
                {
                    Directory.CreateDirectory(docsPath);
                }

                File.WriteAllText(offsetFilePath, offset.ToString());
                LogVerbose("Значение offset успешно сохранено в файл: {0}", offsetFilePath);
            }
            catch (Exception ex)
            {
                Log("Ошибка при сохранении offset в файл: {0}", ex.Message);
            }
        }
        internal static void SendMarkov(ChatInfo chat)
        {
            try
            {
                Random rng = new Random();
                SendMessage(chat.ChatId, chat.MarkovMessages.CreateText(rng.Next(20) + 1));
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
            }
        }
        internal static void SendQuote(ChatInfo chat)
        {
            SendQuote(chat, chat.Words, false);
        }
        internal static void SendQuote(ChatInfo chat, string[] words)
        {
            Dictionary<string, int> uniformWordsDistribution = new();
            foreach (string word in words)
            {
                uniformWordsDistribution.TryAdd(word, 1);
            }
            SendQuote(chat, uniformWordsDistribution);
        }
        internal static void SendQuote(ChatInfo chat, Dictionary<string, int> words, bool manual = true)
        {
            SearchResult searchResult = chat.QuotationsFinder.Search(words);

            switch (searchResult.status)
            {
                case SearchStatus.NotFound:
                    string text;
                    if (manual)
                    {
                        text = $"{QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber]} на тему «{string.Join(" ", words.Keys)}» не найдены.\n\nЕстественно, нормальные люди о таком не говорят... Попробуй что-то более адекватное.";
                    }
                    else
                    {
                        text = $"Вы недостаточно много обсуждаете {QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber]}, поэтому не получите крутую тематическую цитату.";
                    }
                    SendMessage(chat.ChatId, text);
                    break;
                case SearchStatus.NoSource:
                    string description = QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber];
                    SendMessage(chat.ChatId, $"Файл «{description}» пока что не готов. Выберите другой файл.");
                    break;
                case SearchStatus.Found:
                    bool messageSourceFound = false;
                    foreach (KeyValuePair<int, string[]> message in chat.Messages)
                    {
                        if (message.Value.Contains(searchResult.word))
                        {
                            Log("Найдено слово {0} в сообщении {1}. Составляю цитату и отправляю её в ответ на это сообщение", searchResult.word, message.Key);
                            SendMessage(chat.ChatId,
                                ComposeMessageWithQuotation(searchResult, chat.ChatId),
                                replyParameters: message.Key);
                            messageSourceFound = true;
                            break;
                        }
                    }
                    if (!messageSourceFound)
                    {
                        Log("Не найдено сообщение, в котором есть слово {0}. Отправляю сообщение без ответа.", searchResult.word);
                        SendMessage(chat.ChatId, ComposeMessageWithQuotation(searchResult, chat.ChatId));
                    }
                    break;
                default:
                    Log("!!!!!!Недостижимый код!!!!!");
                    break;
            }
            if (!manual)
            {
                chat.Words.Clear();
                chat.Messages.Clear();
                Log("Чат {0} очищен от сообщений и слов.", chat.ChatId);
            }
        }
        internal static bool NeedsExplaining(string text)
        {
            string[] patterns =
            {
                @"не\s+понимаю",
                @"не\s+могу\s+понять",
                @"не\s+улавливаю",
                @"не\s+схватываю",
                @"не\s+могу\s+уяснить",
                @"не\s+могу\s+осмыслить",
                @"не\s+могу\s+разобраться",
                @"не\s+могу\s+вникнуть",
                @"мне\s+неясно",
                @"мне\s+непонятно",
                @"я\s+не\s+понимаю,\s+что\s+вы\s+имеете\s+в\s+виду",
                @"я\s+не\s+понимаю,\s+о\s+чем\s+вы",
                @"я\s+не\s+понимаю,\s+к\s+чему\s+вы\s+клоните",
                @"я\s+не\s+понимаю,\s+что\s+от\s+меня\s+хотят",
                @"это\s+выше\s+моего\s+понимания",
                @"это\s+вне\s+моей\s+компетенции",
                @"это\s+для\s+меня\s+загадка",
                @"моя\s+мысль\s+не\s+поспевает\s+за\s+вашей",
                @"я\s+теряю\s+нить",
                @"вы\s+меня\s+теряете",
                @"я\s+запутался",
                @"я\s+запуталась",
                @"я\s+в\s+замешательстве",
                @"я\s+в\s+тупике",
                @"я\s+не\s+могу\s+сообразить",
                @"мои\s+мозги\s+не\s+варят",
                @"мой\s+мозг\s+отказывается\s+это\s+обрабатывать",
                @"простите,\s+я\s+не\s+совсем\s+понимаю",
                @"извините,\s+я\s+не\s+вполне\s+понимаю",
                @"не\s+могли\s+бы\s+вы\s+пояснить\?",
                @"будьте\s+добры,\s+объясните\s+еще\s+раз",
                @"пожалуйста,\s+уточните",
                @"не\s+понял,\s+повторите,\s+пожалуйста",
                @"не\s+поняла,\s+повторите,\s+пожалуйста",
                @"можете\s+объяснить\s+иначе\?",
                @"я\s+не\s+уверен,\s+что\s+правильно\s+понял",
                @"я\s+не\s+уверен,\s+что\s+правильно\s+поняла",
                @"я\s+не\s+уверена,\s+что\s+правильно\s+понял",
                @"я\s+не\s+уверена,\s+что\s+правильно\s+поняла",
                @"что\s+именно\s+вы\s+подразумеваете\?",
                @"в\s+каком\s+смысле\?",
                @"что\s+вы\s+хотите\s+этим\s+сказать\?",
                @"к\s+чему\s+это\?",
                @"вы\s+о\s+чем\?",
                @"не\s+уловил,\s+простите",
                @"не\s+уловила,\s+простите",
                @"смысл\s+ускользает\s+от\s+меня",
                @"можете\s+разжевать\?",
                @"растолкуйте,\s+пожалуйста",
                @"ничего\s+не\s+понимаю\!",
                @"я\s+совершенно\s+не\s+понимаю\!",
                @"я\s+абсолютно\s+не\s+понимаю\!",
                @"я\s+в\s+ступоре",
                @"я\s+в\s+ауте",
                @"я\s+не\s+въезжаю",
                @"я\s+не\s+втыкаю",
                @"я\s+не\s+догоняю",
                @"я\s+не\s+капитан",
                @"это\s+китайская\s+грамота\s+для\s+меня",
                @"это\s+темный\s+лес\s+для\s+меня",
                @"это\s+для\s+меня\s+дремучий\s+лес",
                @"мозг\s+сломал",
                @"мозги\s+кипят",
                @"голова\s+кругом\s+идет",
                @"голова\s+не\s+соображает",
                @"до\s+меня\s+не\s+доходит",
                @"не\s+доходит",
                @"мысль\s+не\s+дошла",
                @"я\s+не\s+могу\s+этого\s+постичь",
                @"не\s+укладывается\s+у\s+меня\s+в\s+голове",
                @"не\s+укладывается\s+в\s+моей\s+голове",
                @"это\s+не\s+умещается\s+в\s+моем\s+сознании",
                @"я\s+не\s+могу\s+этого\s+осознать",
                @"я\s+не\s+могу\s+этого\s+переварить",
                @"это\s+слишком\s+сложно\s+для\s+меня",
                @"я\s+не\s+способен\s+это\s+понять",
                @"я\s+не\s+способна\s+это\s+понять",
                @"мои\s+познания\s+здесь\s+равны\s+нулю",
                @"я\s+не\s+понимаю,\s+как\s+это\s+возможно",
                @"я\s+не\s+вижу\s+в\s+этом\s+смысла",
                @"это\s+не\s+имеет\s+для\s+меня\s+смысла",
                @"это\s+лишено\s+смысла",
                @"я\s+отказываюсь\s+это\s+понимать\!",
                @"это\s+выше\s+моего\s+сочувствия/восприятия",
                @"мне\s+это\s+чуждо",
                @"я\s+не\s+разделяю\s+вашу\s+точку\s+зрения",
                @"это\s+абсурд\!",
                @"это\s+бред\!",
                @"чепуха\!",
                @"глупости\!",
                @"бессмыслица\!",
                @"мы\s+говорим\s+на\s+разных\s+языках",
                @"что\?",
                @"чего\?",
                @"\sа\?",
                @"\sэ\?",
                @"\sм\?",
                @"как-как\?",
                @"чё\?",
                @"шо\?",
                @"\?\?\?",
                @"не\s+понял",
                @"не\s+поняла",
                @"не\s+ясно",
                @"сложно",
                @"запутанно",
                @"трудно",
                @"понятия\s+не\s+имею",
                @"без\s+понятия",
                @"хм\.\.\.",
                @"пожимание\s+плечами",
                @"выразительный\s+взгляд,\s+полный\s+непонимания",
                @"разведение\s+руками",
                @"мне\s+это\s+невдомек",
                @"мне\s+сие\s+не\s+ясно",
                @"сей\s+вопрос\s+остается\s+для\s+меня\s+неразрешенным",
                @"я\s+не\s+постигаю",
                @"смысл\s+от\s+меня\s+сокрыт",
                @"я\s+не\s+могу\s+узреть\s+истинный\s+смысл",
                @"это\s+представляется\s+мне\s+неясным",
                @"у\s+меня\s+не\s+сложилось\s+понимание",
                @"я\s+не\s+могу\s+найти\s+логику\s+в\s+этом",
                @"я\s+не\s+усматриваю\s+здесь\s+логики",
                @"мое\s+восприятие\s+этого\s+ограничено",
                @"это\s+вне\s+рамок\s+моего\s+понимания",
                @"у\s+меня\s+в\s+голове\s+каша",
                @"все\s+смешалось\s+в\s+моей\s+голове",
                @"смысл\s+тонет\s+в\s+тумане",
                @"я\s+как\s+в\s+тумане",
                @"точка,\s+я\s+не\s+понял",
                @"точка,\s+я\s+не\s+поняла",
                @"моя\s+чашка\s+не\s+накрывает\s+этого",
                @"это\s+не\s+укладывается\s+в\s+моей\s+голове",
                @"я\s+не\s+могу\s+этого\s+просечь",
            };

            string combinedPattern = string.Join("|", patterns);

            var combinedRegex = new Regex(combinedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            bool result = combinedRegex.IsMatch(text);
            if (result) Log("Сообщение требует объяснения:\n{0}", text);
            return result;
        }
        internal static void Explain(Message msg)
        {
            ChatInfo chat = Chats[msg.Chat.Id];
            LogVerbose("Попытка объяснить для чата {0}; {1}, {2}", msg.Chat, chat.explanationsToday, chat.explanationsDay);
            if (chat.explanationsToday <= dailyExplanationsPerChat || chat.explanationsDay.Date != DateTime.Now.Date)
            {
                Log("Объяснение запущено для {0}", msg.Chat);
                if (chat.explanationsDay.Date != DateTime.Now.Date)
                {
                    chat.explanationsToday = 0;
                    chat.explanationsDay = DateTime.Now.Date;
                }
                chat.explanationsToday++;
                Task t = new Task(async () =>
                {
                    string userPrompt = string.Join("\n", chat.last5msgs),
                        message;

                    LogVerbose("Запрос для объяснения:\n{0}", userPrompt);

                    message = await chat.deepseekWrapper.GetExplanation(userPrompt);

                    LogVerbose("Ответ от Deepseek получен:\n{0}", message);

                    SendMessage(chat.ChatId, message, msg.Id);
                });
                t.Start();
                tasks.Add(t);
            }
            else
            {
                SendMessage(chat.ChatId, $"Я бы очень хотел пояснить, что вы тут не понимаете, но ваш чат израсходовал бюджет на {dailyExplanationsPerChat} менсплейнингов в день(((("); 
            }
        }
        /// <summary>
        /// Отправляет сообщение во все чаты, в которых работает бот.
        /// </summary>
        /// <param name="message">Сообщение</param>
        internal static void SendMessageToAllChats(string message)
        {
            foreach (long chatid in Chats.Keys)
            {
                SendMessage(chatid, message + "\n\n/ryabkin");
            }
        }
    }
}
