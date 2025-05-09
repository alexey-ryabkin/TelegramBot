// Убрать лишний конструктор. Пусть всё будет в главном методе

using System.Threading.Tasks;
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
                Thread.Sleep(10000);
                var updates = await bot.GetUpdates(offset, timeout: 2);
                foreach (var update in updates)
                {
                    if (offset is null || offset <= update.Id) offset = update.Id + 1;
                    try
                    {
                        Log("Получено обновление");
                        switch (update)
                        {
                            case { Message: { } msg }: await HandleMessage(msg); break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("‼️ CRITICAL ERROR ‼️");
                        Log(ex.ToString());
                    }
                    if (cts.IsCancellationRequested) break;
                }
            }
            //bot!.OnMessage += ManualRespond;


            while (input != "exit")
            {
                Thread.Sleep(5000);
            }
            cts.Cancel();
            Log("Работа программы завершается успешно с кодом 0.");
            return 0;
        }
    }
}
