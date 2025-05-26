using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaimiraGames;
using Telegram.Bot.Types;
using static TelegramBot.Logger;
using static TelegramBot.MarkovChain;
using static TelegramBot.QuotationsFinder;
using static TelegramBot.Utils;

namespace TelegramBot
{
    /// <summary>
    /// Класс, хранящий информацию о конкретном чате. Доступ ко всем экземплярам возможен через статическое поле Chats.
    /// </summary>
    internal class ChatInfo
    {
        /// <summary>
        /// Словарь чатов, в которых работает бот. Ключ — ChatId, значение — экземпляр класса ChatInfo.
        /// </summary>
        internal static Dictionary<long, ChatInfo> Chats { get; set; } = new();
        internal enum RandomAction : byte
        {
            Nothing = 0,
            MaoQuote = 1,
            MarkovChain = 2
        }
        internal static Dictionary<RandomAction, string> RandomActionDescriptions = new Dictionary<RandomAction, string>
        {
            { RandomAction.Nothing,      "Ничего не делать" },
            { RandomAction.MaoQuote,     "Отправить цитату великого человека" },
            { RandomAction.MarkovChain,  "Отправить сообщение" },
        };
        internal static Dictionary<RandomAction, string> RandomActionSettingCommand = new Dictionary<RandomAction, string>
        {
            { RandomAction.Nothing,      "lazyness" },
            { RandomAction.MaoQuote,     "quotecoeff" },
            { RandomAction.MarkovChain,  "msgcoeff" },
        };
        /// <summary>
        /// Идентификатор чата, тип значения long по требованию библиотеки.
        /// </summary>
        [JsonInclude]
        internal long ChatId { get; init; }
        /// <summary>
        /// Словарь, в котором ключ — слово, а значение — количество раз, когда это слово встречалось в чате.
        /// </summary>
        [JsonInclude]
        internal Dictionary<string, int> Words { get; set; } = new();
        /// <summary>
        /// Словарь, в котором ключ — идентификатор сообщения, а значение — массив слов, которые были найдены в этом сообщении.
        /// Слово — слово длинее 4 букв в нижнем регистре.
        /// </summary>
        [JsonInclude]
        internal Dictionary<int, string[]> Messages { get; set; } = new();
        /// <summary>
        /// Экземпляр класса QuotationsFinder, который ищет цитаты в загруженной для него базе цитат.
        /// </summary>
        internal QuotationsFinder QuotationsFinder { get; set; }
        [JsonInclude]
        internal int QuotationsFileNumber { get; set; }
        [JsonInclude]
        internal bool ShortenQuotes { get; set; }
        [JsonInclude]
        internal MarkovChain MarkovMessages { get; set; }
        [JsonInclude]
        internal Dictionary<RandomAction, int> RandomActionCoefficients { get; set; }
        [JsonInclude]
        internal DateTime LastMessage { get; set; }
        [JsonInclude]
        internal int TimeoutSec { get; set; }
        internal WeightedList<RandomAction> RandomActionGenerator { get; set; }
        public ChatInfo(long ChatId)
        {
            if (Chats.ContainsKey(ChatId))
            {
                Log("Чат {0} уже есть в словаре обслуживаемых чатов. Программа работает некорректно.", ChatId);
                throw new ApplicationException("Чат уже есть в словаре обслуживаемых чатов.");
            }
            else
            {
                this.ChatId = ChatId;
                Chats.Add(ChatId, this);
                QuotationsFinder = GetQuotationsFinder();
                QuotationsFileNumber = QuotationsFinder.fileNumber;
                ShortenQuotes = true;
                MarkovMessages = new MarkovChain();
                LastMessage = new DateTime();
                RandomActionCoefficients = new Dictionary<RandomAction, int>
                {
                    { RandomAction.Nothing,        66 },
                    { RandomAction.MaoQuote,       3 },
                    { RandomAction.MarkovChain,    30 },
                };
                // Строка для подавления предупреждения
                RandomActionGenerator = new WeightedList<RandomAction>();
                UpdateRAG();
                TimeoutSec = 30;
                Log("Чат {0} добавлен в словарь обслуживаемых чатов.", ChatId);
            }
        }
        [JsonConstructor]
        public ChatInfo(long ChatId,
            Dictionary<string, int> Words, 
            Dictionary<int, string[]> Messages,
            string QuotationsFileName,
            bool ShortenQuotes,
            MarkovChain MarkovMessages,
            DateTime LastMessage,
            Dictionary<RandomAction, int> RandomActionCoefficients,
            int TimeoutSec)
        {
            if (Chats.ContainsKey(ChatId))
            {
                Log("Чат {0} уже есть в словаре обслуживаемых чатов. Десериализация работает некорректно.", ChatId);
                throw new ApplicationException("Чат уже есть в словаре обслуживаемых чатов.");
            }
            else
            {
                this.ChatId = ChatId;
                this.Words = Words;
                this.Messages = Messages;
                Chats.Add(ChatId, this);
                QuotationsFinder = GetQuotationsFinder(QuotationsFileName);
                QuotationsFileNumber = QuotationsFinder.fileNumber;
                this.ShortenQuotes = ShortenQuotes;
                this.MarkovMessages = MarkovMessages;
                this.LastMessage = LastMessage;
                this.RandomActionCoefficients = RandomActionCoefficients;
                // Строка для подавления предупреждения
                RandomActionGenerator = new WeightedList<RandomAction>();
                UpdateRAG();
                this.TimeoutSec = TimeoutSec;
                Log("Чат {0} загружен с диска в словарь обслуживаемых чатов.", ChatId);
            }
        }
        private void UpdateRAG()
        {
            WeightedListItem<RandomAction>[] wlis = new WeightedListItem<RandomAction>[RandomActionCoefficients.Count];
            int i = 0;
            foreach (RandomAction key in RandomActionCoefficients.Keys)
            {
                wlis[i] = new WeightedListItem<RandomAction>(key, RandomActionCoefficients[key]);
                i++;
            }
            WeightedList<RandomAction> wl = new WeightedList<RandomAction>(wlis);
            wl.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
            RandomActionGenerator = wl;
        }
        public void AddMessage(int messageId, string[] words)
        {
            if (Messages.ContainsKey(messageId))
            {
                Log("Сообщение {0} для чата {1} уже есть в словаре. Пропускаю.", messageId, ChatId);
                return;
            }
            else
            {
                Messages.Add(messageId, words);
                foreach (var word in words)
                {
                    if (Words.ContainsKey(word))
                    {
                        Words[word]++;
                    }
                    else
                    {
                        Words.Add(word, 1);
                    }
                }
                foreach (KeyValuePair<string, int> pair in Words)
                {
                    //Log("Слово {0} встречается {1} раз в чате {2}", pair.Key, pair.Value, ChatId);
                }
            }
        }
        /// <summary>
        /// Сохраняет экземпляр базы данных цитат в файл JSON по указанному пути с индентацией и без экранирования. Существующий файл будет перезаписан.
        /// </summary>
        /// <param name="fileName">Путь, название файла.</param>
        internal static void SerializeToJSON(string fileName)
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(Chats.Values, options);
                File.WriteAllText(fileName, jsonString);
            }
            catch
            {
                Log("Сохранение ChatInfo в файл {0} не удалось.", fileName);
            }
        }
        internal static void LoadFromJSON(string fileName)
        {
            try
            {
                string jsonString = File.ReadAllText(fileName);
                JsonSerializer.Deserialize<List<ChatInfo>>(jsonString);
                Log("ChatInfo успешно загружены из файла {0}.", fileName);
            }
            catch
            {
                Log("Загрузка ChatInfo из файла {0} не удалась.", fileName);
            }
        }
        internal static void SetQuotationsFile(long chatId, string fileNumber)
        {
            if (Chats.ContainsKey(chatId))
            {
                if (Int32.TryParse(fileNumber, out int fileNumberParsed) && fileNumberParsed >= 0)
                {
                    try
                    {
                        Chats[chatId].QuotationsFinder = GetQuotationsFinder(availableQuotationFiles[fileNumberParsed]);
                        Chats[chatId].QuotationsFileNumber = fileNumberParsed;

                        SendMessage(chatId, $"Файл цитат успешно установлен. Теперь будут говорить {availableQuotationFileDescriptions[fileNumberParsed]}.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log("В SetQuotationsFile передано корректное значение {0}, но что-то пошло не так.", fileNumber);
                        SendMessage(chatId, $"Не получилось назначить настройке значение {fileNumber}. Попробуйте указанное в команде /help.");
                    }
                }
                Log("В SetQuotationsFile передано некорректное значение {0}! Отвечаю грубостью.", fileNumber);
                SendMessage(chatId, $"Кто-то глупенький)) {fileNumber} — не целое положительное число. Попробуй ещё раз.");
            }
            else
            {
                Log($"{new string('!', 30)}По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
            }
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Chat ID: {ChatId}");
            sb.AppendLine("Words Frequency:");
            foreach (KeyValuePair<string, int> word in Words)
            {
                sb.AppendLine($"  {word.Key}: {word.Value}");
            }
            sb.AppendLine("Messages:");
            foreach (KeyValuePair<int, string[]> message in Messages)
            {
                sb.AppendLine($"  Message ID {message.Key}: [{string.Join(", ", message.Value)}]");
            }
            sb.AppendLine($"QuotationsFileName: {availableQuotationFileDescriptions[QuotationsFileNumber]}");
            sb.Append($"ShortenQuotes: {ShortenQuotes}");
            return sb.ToString();
        }

        internal static void TurnOnShortening(long chatId)
        {
            if (Chats.ContainsKey(chatId))
            {
                ChatInfo chat = Chats[chatId];
                if (chat.ShortenQuotes)
                {
                    SendMessage(chatId, $"Сокращение цитат уже включено.");
                }
                else
                {
                    chat.ShortenQuotes = true;
                    SendMessage(chatId, $"Сокращение цитат успешно включено. Теперь Мао будет говорить поменьше.");
                }
            }
            else
            {
                Log("По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
            }
        }

        internal static void TurnOffShortening(long chatId)
        {
            if (Chats.ContainsKey(chatId))
            {
                ChatInfo chat = Chats[chatId];
                if (chat.ShortenQuotes)
                {
                    chat.ShortenQuotes = false;
                    SendMessage(chatId, $"Сокращение цитат успешно выключено. Теперь можно будет насладиться великой мыслью выликого человека.");
                }
                else
                {
                    SendMessage(chatId, $"Сокращение цитат уже выключено.");
                }
            }
            else
            {
                Log("По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
            }
        }
        internal static void SetCoeff(long chatId, string coeffStr, RandomAction action)
        {
            if (Chats.ContainsKey(chatId))
            {
                if (Int32.TryParse(coeffStr, out int coeff) && coeff >= 0)
                {
                    try
                    {
                        Chats[chatId].RandomActionCoefficients[action] = coeff;
                        SendMessage(chatId, $"{RandomActionDescriptions[action]}: теперь частота {coeff}.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log("В SetLazyness передано корректное значение {0}, но что-то пошло не так.", coeffStr);
                        SendMessage(chatId, $"Не получилось назначить настройке значение {coeffStr}. Попробуйте другое.");
                        return;
                    }
                }
                Log("В lazynessStr передано некорректное значение {0}! Отвечаю грубостью.", coeffStr);
                SendMessage(chatId, $"Кто-то глупенький)) {coeffStr} — не целое неотрицательное число. Попробуй ещё раз.");
            }
            else
            {
                Log("По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
            }
        }
        internal static void SetTimeout(long chatId, string coeffStr)
        {
            if (Chats.ContainsKey(chatId))
            {
                if (Int32.TryParse(coeffStr, out int coeff) && coeff >= 0)
                {
                    try
                    {
                        Chats[chatId].TimeoutSec = coeff;
                        SendMessage(chatId, $"Теперь бот будет писать не чаще чем раз в {coeff} секунд.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log("В SetTimeout передано корректное значение {0}, но что-то пошло не так.", coeffStr);
                        SendMessage(chatId, $"Не получилось назначить настройке значение {coeffStr}. Попробуйте другое.");
                        return;
                    }
                }
                Log("В SetTimeout передано некорректное значение {0}! Отвечаю грубостью.", coeffStr);
                SendMessage(chatId, $"Кто-то глупенький)) {coeffStr} — не целое неотрицательное число. Попробуй ещё раз.");
            }
            else
            {
                Log("По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
            }
        }
    }
}
