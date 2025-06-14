﻿using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using static Logger.Logger;
using static TelegramBot.Utils;
using static TelegramBot.ChatInfo;

namespace TelegramBot
{
    internal partial class TelegramBot
    {
        internal static bool optionsVerbose = false;
        internal static string apiKey = string.Empty;
        internal static TelegramBotClient? bot = null;
        internal static CancellationTokenSource cts = new CancellationTokenSource();
        internal static string input = string.Empty;
        internal static string docsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), typeof(TelegramBot).Name);
        internal static string apiKeyFilePath = Path.Combine(docsPath, @"apiKey.txt");
        internal static string chatInfoFilePath = Path.Combine(docsPath, @"chatInfo.json");
        internal static string offsetFilePath = Path.Combine(docsPath, @"offset.txt");
        internal static int? offset = null;
        internal static List<Task> tasks = new List<Task>();

        internal static string deepseekAPIKey = string.Empty;
        internal static string deepseekAPIKeyFilePath = Path.Combine(docsPath, @"deepseekAPIKey.txt");

        internal static async Task HandleUpdates()
        {
            while (!cts.IsCancellationRequested && bot is not null)
            {
                Update[]? updates = null;
                try
                {
                    updates = await bot.GetUpdates(offset, timeout: 2);
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
                catch (Telegram.Bot.Exceptions.RequestException ex)
                {
                    Log("Произошла ошибка:\n{0}", ex);
                }
                if (updates is not null && updates.Length != 0)
                {
                    SaveAllToFile();
                }
                //tasks.RemoveAll(t => t.IsCompleted);
            }
        }
        /// <summary>
        /// Сохраняет все необходимые данные в структуру ChatInfo. 
        /// Обновляет сумму слов, словарь частоты появления слов, словарь ID сообщения — список слов.
        /// </summary>
        /// <param name="msg">Сообщение Telegram API, в котором есть текст.</param>
        internal static void HandleMessage(Message msg)
        {
            long chatId = msg.Chat.Id;
            string text;
            Random rng = new Random();
            if (!Chats.ContainsKey(chatId)) new ChatInfo(chatId);
            ChatInfo chat = Chats[chatId];

            if (chat.Banned) return;

            switch (msg)
            {
                case { Text: { } temp }: text = temp; break;
                case { Caption: { } temp }: text = temp; break;
                default:
                    LogVerbose("Получено обновление, в котором нет текста: {0}", msg.Type.ToString() ?? "NULL");
                    return;
            }
            LogVerbose("Получено обновление, в котором есть текст длиной {0} символов.", text.Length);

            // Обработка назначения настроек в раздельных сообщениях
            if (chat.SettingsState != Setting.None)
            {
                chat.EndChangingSetting(text);
            }

            // Хранение последних X сообщений
            chat.Enqueue(msg);

            // Сохранение информации для цитат
            string[] words = ExtractWords(text);
            chat.AddMessage(msg.MessageId, words);

            // Сохранение данных для генерации цепей Маркова. Видимо, исключая пересылы.
            if (msg.ForwardOrigin is null)
            {
                chat.MarkovMessages.AddText(text);
                LogVerbose("Текст добавлен в цепь Маркова.");
            }

            // Случайное действие, если бот молчит более заданного количества секунд
            TimeSpan untilNewMessage = new TimeSpan(0, 0, chat.TimeoutSec) - (DateTime.Now - chat.LastMessage);
            if (untilNewMessage.Ticks <= 0)
            {
                RandomAction roll = chat.RandomActionGenerator.Next();
                switch (roll)
                {
                    case RandomAction.MaoQuote:
                        Log("Для чата {0} выпал {1}, отправляю цитату из выбранного файла.", msg.Chat, roll);
                        SendQuote(chat);
                        chat.LastMessage = DateTime.Now;
                        break;
                    case RandomAction.MarkovChain:
                        Log("Для чата {0} выпал {1}, отправляю генерацию Маркова.", msg.Chat, roll);
                        SendMarkov(chat);
                        chat.LastMessage = DateTime.Now;
                        break;
                    case RandomAction.Nothing:
                        Log("Для чата {0} выпал {1}, ничего не делаю.", msg.Chat, roll);
                        break;
                }
            }
            else
            {
                Log("Для чата {0} пока рано писать сообщение. Остапось ещё {1} секунд.", msg.Chat, untilNewMessage.TotalSeconds);
            }

            // Обработка команд
            switch (text)
            {
                case string match when Regex.IsMatch(match.ToLower(), @"^\/quotationsfile(@littleryabot)?\s+-?\d+$"):
                    SetQuotationsFile(msg.Chat.Id, Regex.Match(text, @"-?\d+").Value);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/quotationsfile(@littleryabot)?$"):
                    chat.StartChangingSetting(Setting.QuotationsFile);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/help(@littleryabot)?$"):
                    SendHelp(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/settings(@littleryabot)?$"):
                    SendSettings(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/turnonshortening(@littleryabot)?$"):
                    TurnOnShortening(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/turnoffshortening(@littleryabot)?$"):
                    TurnOffShortening(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/sendquote(@littleryabot)?$"):
                    Log("Отправка цитаты в чате {0} запущена вручную.", msg.Chat);
                    SendQuote(Chats[msg.Chat.Id]);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/sendquote(@littleryabot)?((?:\s+\S+)+)$"):
                    string a = Regex.Match(text, @"((?:\s+\S+)+)").Value;
                    string[] b = a.Trim().Split();
                    SendQuote(Chats[msg.Chat.Id], b);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/lazyness(@littleryabot)?\s+-?\d+$"):
                    SetCoeff(msg.Chat.Id, Regex.Match(text, @"-?\d+").Value, RandomAction.Nothing);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/lazyness(@littleryabot)?$"):
                    chat.StartChangingSetting(Setting.Lazyness);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/quotecoeff(@littleryabot)?\s+-?\d+$"):
                    SetCoeff(msg.Chat.Id, Regex.Match(text, @"-?\d+").Value, RandomAction.MaoQuote);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/quotecoeff(@littleryabot)?$"):
                    chat.StartChangingSetting(Setting.Quotecoeff);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/msgcoeff(@littleryabot)?\s+-?\d+$"):
                    SetCoeff(msg.Chat.Id, Regex.Match(text, @"-?\d+").Value, RandomAction.MarkovChain);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/msgcoeff(@littleryabot)?$"):
                    chat.StartChangingSetting(Setting.Msgcoeff);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/timeout(@littleryabot)?\s+-?\d+$"):
                    SetTimeout(msg.Chat.Id, Regex.Match(text, @"-?\d+").Value);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/timeout(@littleryabot)?$"):
                    chat.StartChangingSetting(Setting.Timeout);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/explain(@littleryabot)?") || NeedsExplaining(text):
                    Explain(msg);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/ryabkin(@littleryabot)?"):
                    SendRyabkin(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/start(@littleryabot)?"):
                    SendStart(msg.Chat.Id);
                    break;
                case string match when Regex.IsMatch(match.ToLower(), @"^\/github(@littleryabot)?"):
                    SendGithub(msg.Chat.Id);
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">verbose — дополнительный лог</param>
        /// <returns>
        /// 0 — корректное заврешение работы, 1 — отсутствует ключ API, 2 — ключ API некорректен, 3 — ошибка соединения.
        /// </returns>
        private static void Remove0s()
        {
            foreach (ChatInfo chat in Chats.Values)
            {
                chat.MarkovMessages.Remove0();
            }
        }
        static async Task<int> Main(string[] args)
        {
            int code = await Startup(args);
            if (code != 0) return code;

            // Логика работы
            Task mainCycle = HandleUpdates();


            while (input != "exit")
            {
                input = GetInput();
                switch (input)
                {
                    case "print chatinfo":
                        foreach (ChatInfo chat in Chats.Values)
                        {
                            Log(chat.ToString());
                        }
                        break;
                    case "load chatinfo":
                        LoadFromJSON(chatInfoFilePath);
                        break;
                    case "save chatinfo":
                        SerializeToJSON(chatInfoFilePath);
                        break;
                    case "remove 0":
                        Remove0s();
                        break;
                    case string match when Regex.IsMatch(match.ToLower(), @"^send to all "):
                        SendMessageToAllChats(match.Replace("send to all ", ""));
                        break;
                    case string match when Regex.IsMatch(match.ToLower(), @"^send release message"):
                        SendMessageToAllChats(
                            """
                            Всем привет! Бот готов.

                            Чтобы узнать о его функциях, нажмите на /start
                            """);
                        break;
                    case string match when Regex.IsMatch(match.ToLower(), @"^send to ktap "):
                        SendMessageToChat(Chats[-1001478044575], match.Replace("send to all ", ""));
                        break;

                }
                //Log(Chats[-1001478044575].MarkovMessages.Next);
            }
            cts.Cancel();
            await mainCycle;
            Task.WaitAll(tasks.ToArray());
            Log("Работа программы завершается успешно с кодом 0.");
            return 0;
        }

    }
}
