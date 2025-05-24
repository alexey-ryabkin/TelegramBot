using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;
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
            Regex regex = new Regex("[а-яА-Я]+");

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

                Городецкий, верни удочку.
                
                """;
            SendMessage(chatId, message);
        }
        internal static void SendSettings(long chatId)
        {
            ChatInfo chat = ChatInfo.Chats[chatId];
            string message = string.Format(
                "Период Мао: {0} уникальных длинных слов. Текущее количество слов: {1}.\n" +
                "/maoperiod@littleryabot N,\n" +
                "где N — натуральное число.\n\n" +
                "Набор цитат: {2}.\n" +
                "/quotationsfile@littleryabot N,\n" +
                "где N — номер файла.\n" +
                "{3}.\n\n" +
                (chat.ShortenQuotes ? "Цитаты сокращаются до одного предложения.\n" : "Цитаты постятся полностью.\n") +
                (chat.ShortenQuotes ? "/turnoffshortening — отключить сокращение." : "/turnonshortening — включить сокращение."),
                chat.MessageThreshold,
                chat.WordCount,
                QuotationsFinder.availableQuotationFileDescriptions[Array.IndexOf(QuotationsFinder.availableQuotationFiles, chat.QuotationsFileName)],
                QuotationsFinder.quotsDescriptions
                );
            SendMessage(chatId, message);
        }
    }
}
