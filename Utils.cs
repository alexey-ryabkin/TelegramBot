using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using static TelegramBot.ChatInfo;
using static TelegramBot.Logger;
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
            string quotation = string.Empty;
            if (ChatInfo.Chats[chatId].ShortenQuotes)
            {
                quotation = ShortenQuotation(searchResult);
            }
            else 
            { 
                quotation = searchResult.quotation; 
            }
                string result = $"⚡ Актуальная мудрость на тему «{searchResult.word}» ⚡\n\n" +
                    $"{quotation}\n\n" +
                    $"{searchResult.author}. {searchResult.source}.";
            return result;
        }
        internal static string ShortenQuotation(SearchResult searchResult)
        {
            if (searchResult.status != SearchStatus.Found)
            {
                return searchResult.quotation;
            }
            // Кроме последнего символа
            string word = searchResult.word[..^1],
                quote = searchResult.quotation;
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
                
                Привет! Я маленький красный бот.

                Периодически я отправляю в чат тематическую цитату великого человека. Иногда поясняю непонятные моменты. Иногда решаю сказать что-то умное на темы, обсуждаемые в чате.

                Если ты попытаешься удалить меня из чата, то всю твою семью настигнут 70 лет неудач, а все их потомки окажутся гендерфлюидными демибоями. Но тебе ничего плохого не будет, это главное.

                /settings для просмотра и редактирования настроек.
                /help для текущего сообщения. Но ты его и так видишь.
                /sendquote для того, чтобы отправить цитату прямо сейчас.

                Городецкий, верни удочку.
                
                """;
            SendMessage(chatId, message);
        }
        internal static void SendSettings(long chatId)
        {
            ChatInfo chat = Chats[chatId];
            StringBuilder sb = new StringBuilder();
            foreach (RandomAction key in chat.RandomActionCoefficients.Keys)
            {
                // /lazyness 40 — Ничего не делать
                sb.AppendLine($"/{RandomActionSettingCommand[key]} " +
                    $"{chat.RandomActionCoefficients[key]} — " +
                    $"{RandomActionDescriptions[key]}");
            }
            string randomActionsDescriptions = sb.ToString();


            string message = string.Format(
            "Коэффициенты лености:\n" +
            "{0}\n" +
            "где N — натуральное число.\n\n" +
            "Набор цитат: {1}.\n" +
            "/quotationsfile N,\n" +
            "где N — номер файла.\n" +
            "{2}.\n\n" +
            (chat.ShortenQuotes ? "Цитаты сокращаются до одного предложения.\n" : "Цитаты постятся полностью.\n") +
            (chat.ShortenQuotes ? "/turnoffshortening — отключить сокращение.\n\n" : "/turnonshortening — включить сокращение.\n\n") +
            $"/timeout {chat.TimeoutSec} — время между сообщениями, секунд.",
            randomActionsDescriptions,
            QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber],
            QuotationsFinder.quotsDescriptions
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
            SearchResult searchResult = chat.QuotationsFinder.Search(chat.Words);

            switch (searchResult.status)
            {
                case SearchStatus.NotFound:
                    SendMessage(chat.ChatId, $"Вы недостаточно много изучаете {QuotationsFinder.availableQuotationFileDescriptions[chat.QuotationsFileNumber]}, поэтому не получите крутую тематическую цитату.");
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
            chat.Words.Clear();
            chat.Messages.Clear();
            Log("Чат {0} очищен от сообщений и слов.", chat.ChatId);
        }
    }
}
