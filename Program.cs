// Убрать лишний конструктор. Пусть всё будет в главном методе

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mode">0 — локальный режим, 1 — публичный режим, 2 — тестовый режим</param>
    internal class TelegramBot
    {
        private static readonly int mode = 0;
        private string apikey { get; init; }
        public TelegramBotClient Client { get; init; }
        public int status;
        /// <summary>
        /// 
        /// </summary>
        public TelegramBot(CancellationTokenSource cts)
        {
            // Загрузка ключа API из локального хранилища
            apikey = string.Empty;
            if (mode == 0 || mode == 2)
            {
                IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
                apikey = config["APIKey"] ?? string.Empty;
                if (apikey.Length < 1)
                {
                    Console.Write("Ключ API от бота Telegram не сохранён в вашем локальном хранилище. Введите ключ API:");
                    apikey = Console.ReadLine() ?? string.Empty;
                }
            }
            else if (mode == 1)
            {
                Console.Write("Введите ключ API:");
                apikey = Console.ReadLine() ?? string.Empty;
            }
            if (apikey.Length < 1)
            {
                status = 1;
                return;
            }
            else
            {
                try
                {
                    Client = new TelegramBotClient(apikey, cancellationToken: cts.Token);
                }
                catch
                {
                    status = 2;
                    return;
                }
            }
            status = 0;
        }
        public async Task<int> MirrorMessage(Message msg, UpdateType updateType)
        {
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Не используется</param>
        /// <returns>
        /// 0 — корректное заврешение работы, 1 — отсутствует ключ API, 2 — ключ API некорректен.
        /// </returns>
        static async Task<int> Main(string[] args)
        {
            // Создаю связь с ботом
            using CancellationTokenSource cts = new CancellationTokenSource();
            TelegramBot bot = new TelegramBot(cts);
            switch (bot.status) 
            {
                case 0:
                    Console.WriteLine("Маленький красный бот запущен. Введите exit для завершения...");
                    break;
                case 1:
                    Console.WriteLine("Ключ API не был введён. Программа завершает работу.");
                    return 1;
                case 2:
                    Console.WriteLine("Ключ API некорректен. Программа завершает работу.");
                    return 2;
            }

            Telegram.Bot.Types.User test = await bot.Client!.GetMe();
            Console.WriteLine(test);

            Console.ReadLine();
            cts.Cancel();
            return 0;
        }
    }
}
