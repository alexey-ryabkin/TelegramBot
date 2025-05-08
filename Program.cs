using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace TelegramBot
{
    internal class TelegramBot
    {
        static void Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
            Console.WriteLine(config["APIKey"]);
        }
    }
}
