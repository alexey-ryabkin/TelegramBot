using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static TelegramBot.Logger;

namespace TelegramBot
{
    internal class TelegramBot
    {
        private static int mode = 1;
        private static string apikey = string.Empty;
        private static TelegramBotClient? bot = null;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private static string input = string.Empty;

        private async static Task<int> Startup(string[] args)
        {
            string modeDescription;
            switch (mode)
            {
                case 0:
                    modeDescription = "публичный режим, ключ API спрашивается";
                    break;
                case 1:
                    modeDescription = "личный режим, ключ API лежит в секретах";
                    break;
                case 2:
                    modeDescription = "тестовый режим";
                    break;
                default:
                    modeDescription = "неизвестный режим";
                    break;
            }
            if (args.Length > 0)
            {
                Log("Выбранный режим работы {0}: {1}.", args[0], modeDescription);
            }
            else
            {
                Log("Режим работы не указан, запуск в режиме по умолчанию {0}: {1}.", mode, modeDescription);
            }

            if (args.Length > 0)
            {
                try
                {
                    mode = int.Parse(args[0]);
                }
                catch
                {
                    Log("Некорректный режим работы. Продолжаю в обычном режиме.");
                }
            }

            // Загрузка ключа API из локального хранилища
            apikey = string.Empty;
            if (mode > 0)
            {
                Log("Загрузка ключа API из локальных секретов.");
                IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
                apikey = config["APIKey"] ?? string.Empty;
                if (apikey.Length < 1)
                {
                    Log("Ключ API от бота Telegram не сохранён в вашем локальном хранилище. Введите ключ API:");
                    apikey = GetInput();
                }
            }
            else
            {
                Log("Введите ключ API:");
                apikey = GetInput();
            }
            if (apikey.Length < 1)
            {
                Log("Ключ API не был введён. Программа завершает работу с кодом 1.");
                return 1;
            }

            Log("Ключ API получен успешно. Начинается создание связи с ботом.");
            // Ключ API загружен, создаю связь с ботом
            try { bot = new TelegramBotClient(apikey, cancellationToken: cts.Token); }
            catch
            {
                Log("Ключ API некорректен. Программа завершает работу с кодом 2.");
                return 2;
            }

            Log("Связь с ботом создана успешно. Проводится проверка соединения.");
            try
            {
                User test = await bot.GetMe();
                Log($"Подключение к боту @{test.Username} установлено.");
            }
            catch (Exception ex)
            {
                Log($"Ошибка подключения к боту: {ex.Message}.\nПрограмма завершает работу с кодом 3.");
                return 3;
            }
            // Вся работы выполнена успешно, сообщаю об этом.
            return 0;
        }
        public static string GetInput()
        {
            input = Console.ReadLine() ?? string.Empty;
            Log("Пользователь ввёл:" + input);
            if (input == "exit")
            {
                while (true) Thread.Sleep(100000);
            }
            else
            {
                return input;
            }
        }
        public static async Task<int> MirrorMessage(Message msg, UpdateType updateType)
        {
            if (bot is null)
            {
                Log("Ошибка: бот не инициализирован.");
                return 1;
            }
            else
            {
                if (msg.Type != MessageType.Text)
                {
                    Log("Получено сообщение типа {0} в чате {1} без текста. Пропускаю", updateType, msg.Chat);
                    return 0;
                }
                Log("Получено сообщение типа {0} в чате {1} с текстом «{2}»", updateType, msg.Chat, msg.Text);
                if (msg.Text == "/start")
                {
                    Log("Получена команда /start. Отправляю приветственное сообщение в чат {0}.", msg.Chat.Id);
                    await bot.SendMessage(msg.Chat.Id, "Привет! Я бот, который дублирует сообщения.");
                    return 0;
                }
                else if (msg.From is not null && msg.Text is not null)
                {
                    Log("Дублирую собщение в чат {0}.", msg.Chat.Id);
                    await bot.SendMessage(msg.Chat, $"Буквально @{msg.From.Username}: {msg.Text.ToUpper()}");
                    return 0;
                }
                Log("Тип сообщения мне непонятен.");
                return 2;
            }
        }
        public static async Task ManualRespond(Message msg, UpdateType type)
        {
            if (bot is null)
            {
                Log("Ошибка: бот не инициализирован.");
            }
            else
            {
                Log($"Получено сообщение типа {type} в {msg.Chat}. Его текст следующий.\n{msg.Text ?? "Текста нет"}\nВведите ответ:");
                string answer = GetInput();
                Log("Отправляю ответ в чат {0}.", msg.Chat.Id);
                await bot.SendMessage(msg.Chat, answer);
            }
        }
        /// <summary>
        /// Сохраняет все необходимые данные в структуру ChatInfo. 
        /// Обновляет сумму слов, словарь частоты появления слов, словарь ID сообщения — список слов.
        /// </summary>
        /// <param name="msg">Сообщение Telegram API, в котором есть текст.</param>
        public static void HandleMessage(Message msg)
        {
            System.Diagnostics.Debug.Assert(msg.Text is not null);
            long chatId = msg.Chat.Id;
            string[] words = ExtractWords(msg.Text);
            if (!ChatInfo.Chats.ContainsKey(chatId)) new ChatInfo(chatId);
            ChatInfo.Chats[chatId].AddMessage(msg.MessageId, words);
            Log("Обработано сообщение в чате {0}, добавлено {1} слов, всего в чате {2} слов.",
                msg.Chat, 
                words.Length, 
                ChatInfo.Chats[chatId].WordCount);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">0 — публичный режим, ключ API спрашивается, 1 — личный режим, ключ API лежит в секретах 2 — тестовый режим</param>
        /// <returns>
        /// 0 — корректное заврешение работы, 1 — отсутствует ключ API, 2 — ключ API некорректен, 3 — ошибка соединения.
        /// </returns>
        static async Task<int> Main(string[] args)
        {
            int code = await Startup(args);
            if (code != 0) return code;

            Log("Маленький красный бот запущен. Введите exit для завершения...");
            
            // Логика работы
            int? offset = null;
            while (!cts.IsCancellationRequested && bot is not null)
            {
                var updates = await bot.GetUpdates(offset, timeout: 2);
                foreach (var update in updates)
                {
                    if (offset is null || offset <= update.Id) offset = update.Id + 1;
                    try
                    {
                        if (update.Message is not null && update.Message.Text is not null && update.Message.Text.Length > 0)
                        {
                            Log("Получено обновление, в котором есть текст длиной {0}", update.Message.Text.Length);
                            HandleMessage(update.Message);
                        }
                        else Log("Получено обновление, в котором нет текста: {0}", update.Type.ToString() ?? "NULL");
                    }
                    catch (Exception ex)
                    {
                        Log("‼️ CRITICAL ERROR ‼️");
                        Log(ex.ToString());
                    }
                    if (cts.IsCancellationRequested) break;
                }
                foreach(ChatInfo chat in ChatInfo.Chats.Values)
                {
                    if (chat.WordCount > 300)
                    {
                        Log("Количество слов в чате {0} превысило 300. Запускаю модуль поиска цитаты.", chat.chatId);

                    }
                }
                //Если количество слов для любого чата превысило 300, запустить модуль на поиск цитаты Мао
                //Если цитата не найдена, то написать в чат сообщение о недостаточной коммунистическости тем
                //Если цитата найдена
                //Проверить все сообщения и найти среди них содержащее слово, возвращённое модулем
                //Если слово не найдено, то отправить итоговое сообщение без ответа
                //Если слово найдено, то отправить сообщение в ответ на него
                //Составить сообщение, отправить его
                //Очистить число сообщений для данного чата и очистить все сообщения для данного чата, очистить все слова для текущего чата
            }


            while (input != "exit")
            {
                Thread.Sleep(5000);
            }
            cts.Cancel();
            Log("Работа программы завершается успешно с кодом 0.");
            return 0;
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
    }
}
