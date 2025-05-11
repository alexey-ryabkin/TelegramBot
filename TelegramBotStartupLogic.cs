using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static TelegramBot.Logger;
using static TelegramBot.Utils;

namespace TelegramBot
{
    internal partial class TelegramBot
    {
        private static string StartupTryLoadAPIKeyFromFile()
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), apikeyFilePath);
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при загрузке ключа API из файла: {ex.Message}");
            }
            return string.Empty;
        }
        private static void StartupSaveAPIKeyToFile()
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), apikeyFilePath);
                LogVerbose("Сохранение ключа API в файл {0}", filePath);
                File.WriteAllText(filePath, apikey);
                Log("Ключ API сохранён в документы пользователя. Не забудьте удалить файл, если не хотите его сохранять\n{0}", filePath);
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при сохранении ключа API в файл: {ex.Message}");
            }
        }
        private static string StartupLoadAPIKeyFromSecrets()
        {
            LogVerbose("Загрузка ключа API из локальных секретов.");
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
            return config["APIKey"] ?? string.Empty;
        }

        private async static Task<int> Startup(string[] args)
        {
            if (args.Contains("verbose")) optionsVerbose = true;
            apikey = StartupTryLoadAPIKeyFromFile();
            if (apikey.Length < 1)
            {
                apikey = StartupLoadAPIKeyFromSecrets();
                if (apikey.Length < 1)
                {
                    Log("Введите ключ API:");
                    apikey = GetInput();
                    if (apikey.Length < 1)
                    {
                        Log("Ключ API не был введён. Программа завершает работу с кодом 1.");
                        return 1;
                    }
                    else
                    {
                        StartupSaveAPIKeyToFile();
                    }
                }
                else
                {
                    Log("Ключ API загружен из локальных секретов.");
                }
            }
            else
            {
                Log("Ключ API загружен из файла.");
            }

            LogVerbose("Начинается создание связи с ботом.");
            try { bot = new TelegramBotClient(apikey, cancellationToken: cts.Token); }
            catch
            {
                Log("Ключ API некорректен. Программа завершит работу с кодом 2.");
                Console.ReadLine();
                return 2;
            }

            LogVerbose("Связь с ботом создана успешно. Проводится проверка соединения.");
            try
            {
                User test = await bot.GetMe();
                Log($"Подключение к боту @{test.Username} установлено.");
            }
            catch (Exception ex)
            {
                Log($"Ошибка подключения к боту: {ex.Message}.\nПрограмма завершает работу с кодом 3.");
                Console.ReadLine();
                return 3;
            }
            // Вся работы выполнена успешно, сообщаю об этом.
            return 0;
        }
    }
}
