﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Logger.Logger;
using static TelegramBot.Utils;
using static TelegramBot.QuotationsFinder;

namespace TelegramBot
{
    internal partial class TelegramBot
    {
        internal static void LoadAllFromFile()
        {
            if (File.Exists(chatInfoFilePath))
            {
                ChatInfo.LoadFromJSON(chatInfoFilePath);
            }
            if (File.Exists(offsetFilePath))
            {
                try
                {
                    offset = int.Parse(File.ReadAllText(offsetFilePath));
                    Log("Данные offset успешно загружены из файла {0}", offsetFilePath);
                }
                catch (Exception ex)
                {
                    Log("Данные offset не удалось загрузить из файла {0}\n{1}", offsetFilePath, ex);
                }
            }
        }
        private static string StartupTryLoadAPIKeyFromFile()
        {
            try
            {
                if (File.Exists(apiKeyFilePath))
                {
                    string fileContents = File.ReadAllText(apiKeyFilePath);
                    if (fileContents.Length > 0)
                    {
                        Log("Ключ API загружен из документов пользователя. Не забудьте удалить файл, если не хотите его сохранять: {0}", apiKeyFilePath);
                    }
                    return fileContents;
                }
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при загрузке ключа API из файла: {ex.Message}");
            }
            return string.Empty;
        }
        private static string StartupTryLoadDeepseekAPIKeyFromFile()
        {
            try
            {
                if (File.Exists(deepseekAPIKeyFilePath))
                {
                    string fileContents = File.ReadAllText(deepseekAPIKeyFilePath);
                    if (fileContents.Length > 0)
                    {
                        Log("Ключ API DeepSeek загружен из документов пользователя. Не забудьте удалить файл, если не хотите его сохранять: {0}", deepseekAPIKeyFilePath);
                    }
                    return fileContents;
                }
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при загрузке ключа API Deepseek из файла: {ex.Message}");
            }
            return string.Empty;
        }
        private static void StartupSaveAPIKeyToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(apiKeyFilePath));
                LogVerbose("Сохранение ключа API в файл {0}", apiKeyFilePath);
                File.WriteAllText(apiKeyFilePath, apiKey);
                Log("Ключ API сохранён в документы пользователя. Не забудьте удалить файл, если не хотите его сохранять\n{0}", apiKeyFilePath);
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при сохранении ключа API в файл: {ex.Message}");
            }
        }
        private static void StartupSaveDeepseekAPIKeyToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(deepseekAPIKeyFilePath));
                LogVerbose("Сохранение ключа API Deepseek в файл {0}", deepseekAPIKeyFilePath);
                File.WriteAllText(deepseekAPIKeyFilePath, deepseekAPIKey);
                Log("Ключ API Deepseek сохранён в документы пользователя. Не забудьте удалить файл, если не хотите его сохранять\n{0}", apiKeyFilePath);
            }
            catch (Exception ex)
            {
                LogVerbose($"Ошибка при сохранении ключа API Deepseek в файл: {ex.Message}");
            }
        }
        private static string StartupLoadAPIKeyFromSecrets()
        {
            LogVerbose("Загрузка ключа API из локальных секретов.");
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
            return config["APIKey"] ?? string.Empty;
        }
        private static string StartupLoadDeepseekAPIKeyFromSecrets()
        {
            LogVerbose("Загрузка ключа API Deepseek из локальных секретов.");
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<TelegramBot>().Build();
            return config["DeepseekAPIKey"] ?? string.Empty;
        }

        private async static Task<int> Startup(string[] args)
        {
            Console.Write(LogPath);
            if (args.Contains("verbose")) IsLogVerbose = true;
            LogPath = docsPath;
            apiKey = StartupTryLoadAPIKeyFromFile();
            if (apiKey.Length < 1)
            {
                apiKey = StartupLoadAPIKeyFromSecrets();
                if (apiKey.Length < 1)
                {
                    Log("Введите ключ API:");
                    apiKey = GetInput();
                }
                else
                {
                    Log("Ключ API загружен из локальных секретов.");
                }
                if (apiKey.Length < 1)
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
                Log("Ключ API загружен из файла.");
            }
            deepseekAPIKey = StartupTryLoadDeepseekAPIKeyFromFile();
            if (deepseekAPIKey.Length < 1)
            {
                deepseekAPIKey = StartupLoadDeepseekAPIKeyFromSecrets();
                if (deepseekAPIKey.Length < 1)
                {
                    Log("Введите ключ API Deepseek:");
                    deepseekAPIKey = GetInput();
                }
                else
                {
                    Log("Ключ API Deepseek загружен из локальных секретов.");
                }
                if (deepseekAPIKey.Length < 1)
                {
                    Log("Ключ API Deepseek не был введён. Программа завершает работу с кодом 1.");
                    return 1;
                }
                else
                {
                    StartupSaveDeepseekAPIKeyToFile();
                }
            }
            else
            {
                Log("Ключ API Deepseek загружен из файла.");
            }

            LogVerbose("Начинается создание связи с ботом.");
            try { bot = new TelegramBotClient(apiKey, cancellationToken: cts.Token); }
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

            BotCommand[] botCommands =
            {
                //new BotCommand("maoperiod", "Назначает число слов, при достижении которых бот будет подбирать цитату. Параметр — целое положительное число, например, /maoperiod 100."),
                new BotCommand("quotationsfile", "Набор цитат"),
                new BotCommand("help", "Help"),
                new BotCommand("settings", "Настройки"),
                new BotCommand("turnonshortening", "Цитаты кратко"),
                new BotCommand("turnoffshortening", "Цитаты полностью"),
                new BotCommand("sendquote", "Цитата."),
                new BotCommand("lazyness", "0 болтает, 100 молчит"),
                new BotCommand("quotecoeff", "Частота цитат"),
                new BotCommand("msgcoeff", "Частота сообщений"),
                new BotCommand("timeout", "Min перерыв"),
                new BotCommand("explain", "Поясни"),
                new BotCommand("ryabkin", "Божество"),
                new BotCommand("start", "Начало"),
                new BotCommand("github", "Исходный код проекта"),
            };
            await bot.SetMyCommands(botCommands);

            // Создание каталога с настройками
            Directory.CreateDirectory(docsPath);

            // Загрузка предыдущего состояния из файла
            LoadAllFromFile();

            // Вся работы выполнена успешно, сообщаю об этом.
            return 0;
        }
    }
}
