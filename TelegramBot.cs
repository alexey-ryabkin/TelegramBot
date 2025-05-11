using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Passport;
using static TelegramBot.Logger;
using static TelegramBot.Utils;

namespace TelegramBot
{
    internal partial class TelegramBot
    {
        internal static bool optionsVerbose = false;
        private static string apikey = string.Empty;
        internal static TelegramBotClient? bot = null;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private static string input = string.Empty;
        private static string apikeyFilePath = @"\TelegramBot\apikey";
        private static int? offset = null;

        /// <summary>
        /// Сохраняет все необходимые данные в структуру ChatInfo. 
        /// Обновляет сумму слов, словарь частоты появления слов, словарь ID сообщения — список слов.
        /// </summary>
        /// <param name="msg">Сообщение Telegram API, в котором есть текст.</param>
        internal static void HandleMessage(Message msg)
        {
            long chatId = msg.Chat.Id;
            string text;
            switch (msg)
            {
                case { Text: { } temp }: text = temp; break;
                case { Caption: { } temp }: text = temp; break;
                default:
                    Log("Получено обновление, в котором нет текста: {0}", msg.Type.ToString() ?? "NULL");
                    return;
            }
            Log("Получено обновление, в котором есть текст длиной {0} символов.", text.Length);
            string[] words = ExtractWords(text);
            if (!ChatInfo.Chats.ContainsKey(chatId)) new ChatInfo(chatId);
            ChatInfo.Chats[chatId].AddMessage(msg.MessageId, words);
            Log("Обработано сообщение в чате {0}, добавлено {1} слов, всего в чате {2} слов.",
                msg.Chat, 
                words.Length, 
                ChatInfo.Chats[chatId].WordCount);
        }
        internal static void HandleBehaviour(ChatInfo chat)
        {
            if (chat.WordCount > chat.MessageThreshold)
            {
                Log("Количество слов в чате {0} превысило порог. Запускаю модуль поиска цитаты.", chat.chatId);
                SearchResult searchResult = chat.quotationsFinder.Search(chat.Words);

                if (searchResult.status == SearchStatus.NotFound)
                {
                    Task doNotAwait = SendMessage(chat.chatId, "Темы ваших сообщений недостаточно коммунистические. Используйте, пожалуйста, более революционную лексику, иначе к вам не придёт дедушка Мао.");
                }
                else
                {
                    bool messageSourceFound = false;
                    foreach (KeyValuePair<int, string[]> message in chat.Messages)
                    {
                        if (message.Value.Contains(searchResult.word))
                        {
                            Log("Найдено слово {0} в сообщении {1}. Составляю цитату и отправляю её в ответ на это сообщение", searchResult.word, message.Key);
                            Task doNotAwait = SendMessage(chat.chatId,
                                ComposeMessageWithQuotation(searchResult),
                                replyParameters: message.Key);
                            messageSourceFound = true;
                            break;
                        }
                    }
                    if (!messageSourceFound)
                    {
                        Log("Не найдено сообщение, в котором есть слово {0}. Отправляю сообщение без ответа.", searchResult.word);
                        Task doNotAwait = SendMessage(chat.chatId, ComposeMessageWithQuotation(searchResult));
                    }
                }
                chat.Words.Clear();
                chat.WordCount = 0;
                chat.Messages.Clear();
                Log("Чат {0} очищен от сообщений и слов.", chat.chatId);
            }
        }
        internal static async Task HandleUpdates()
        {
            while (!cts.IsCancellationRequested && bot is not null)
            {
                try
                {
                    Update[] updates = await bot.GetUpdates(offset, timeout: 2);
                    foreach (var update in updates)
                    {
                        if (offset is null || offset <= update.Id) offset = update.Id + 1;
                        if (update.Message is not null) HandleMessage(update.Message);
                        else LogVerbose("Получено обновление, в котором нет сообщения: {0}", update.Type.ToString() ?? "NULL");
                        if (cts.IsCancellationRequested) break;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Всё правильно
                }
                foreach (ChatInfo chat in ChatInfo.Chats.Values)
                {
                    if (cts.IsCancellationRequested) break;
                    HandleBehaviour(chat);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">verbose — дополнительный лог</param>
        /// <returns>
        /// 0 — корректное заврешение работы, 1 — отсутствует ключ API, 2 — ключ API некорректен, 3 — ошибка соединения.
        /// </returns>
        static async Task<int> Main(string[] args)
        {
            int code = await Startup(args);
            if (code != 0) return code;

            // Логика работы
            Task mainCycle = HandleUpdates();


            while (input != "exit")
            {
                input = GetInput();
            }
            cts.Cancel();
            await mainCycle;
            Log("Работа программы завершается успешно с кодом 0.");
            return 0;
        }

    }
}
