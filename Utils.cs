using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
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
        internal static string ComposeMessageWithQuotation(SearchResult searchResult)
        {
            string result = $"⚡ Актуальная мудрость на тему «{searchResult.word}» ⚡\n\n" +
                $"{searchResult.quotation}\n\n" +
                $"{searchResult.author}. {searchResult.source}.";
            return result;
        }
        internal async static Task SendMessage(long chatId, string message, ReplyParameters? replyParameters)
        {
            if (bot is null)
            {
                Log("Ошибка: бот не инициализирован.");
            }
            else
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                await bot.SendMessage(chatId, message, replyParameters: replyParameters);
                stopwatch.Stop();
                Log($"Отправка сообщения заняла {stopwatch.ElapsedMilliseconds} миллисекунд.");
            }
        }
        internal async static Task SendMessage(long chatId, string message)
        {
            await SendMessage(chatId, message, null);
        }
    }
}
