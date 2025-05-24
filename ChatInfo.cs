using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot.Types;
using static TelegramBot.Logger;
using static TelegramBot.QuotationsFinder;
using static TelegramBot.Utils;

namespace TelegramBot
{
    /// <summary>
    /// Класс, хранящий информацию о конкретном чате. Доступ ко всем экземплярам возможен чеерз статическое поле Chats.
    /// </summary>
    internal class ChatInfo
    {
        /// <summary>
        /// Словарь чатов, в которых работает бот. Ключ — ChatId, значение — экземпляр класса ChatInfo.
        /// </summary>
        internal static Dictionary<long, ChatInfo> Chats { get; set; } = new();
        /// <summary>
        /// Идентификатор чата, тип значения long по требованию библиотеки.
        /// </summary>
        [JsonInclude]
        internal long ChatId { get; init; }
        /// <summary>
        /// Количество слов, которые были обработаны в чате.
        /// </summary>
        [JsonInclude]
        internal int WordCount { get; set; } = 0;
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
        internal int MessageThreshold { get; set; } = 100;
        [JsonInclude]
        internal string QuotationsFileName { get; set; }
        [JsonInclude]
        internal bool ShortenQuotes { get; set; }
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
                QuotationsFileName = QuotationsFinder.fileName;
                ShortenQuotes = true;
                Log("Чат {0} добавлен в словарь обслуживаемых чатов.", ChatId);
            }
        }
        [JsonConstructor]
        public ChatInfo(long ChatId, 
            int WordCount, 
            Dictionary<string, int> Words, 
            Dictionary<int, string[]> Messages, 
            int MessageThreshold, 
            string QuotationsFileName,
            bool ShortenQuotes)
        {
            if (Chats.ContainsKey(ChatId))
            {
                Log("Чат {0} уже есть в словаре обслуживаемых чатов. Десериализация работает некорректно.", ChatId);
                throw new ApplicationException("Чат уже есть в словаре обслуживаемых чатов.");
            }
            else
            {
                this.ChatId = ChatId;
                this.WordCount = WordCount;
                this.Words = Words;
                this.Messages = Messages;
                this.MessageThreshold = MessageThreshold;
                Chats.Add(ChatId, this);
                QuotationsFinder = GetQuotationsFinder(QuotationsFileName);
                this.QuotationsFileName = QuotationsFinder.fileName;
                this.ShortenQuotes = ShortenQuotes;
                Log("Чат {0} загружен с диска в словарь обслуживаемых чатов.", ChatId);
            }
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
                WordCount += words.Length;
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
        // TODO Настройки
        internal static void SetMessageThreshold(long chatId, string threshold)
        {
            if (Chats.ContainsKey(chatId))
            {
                if (Int32.TryParse(threshold, out int thresholdParsed) && thresholdParsed > 0)
                {
                    try
                    {
                        Chats[chatId].MessageThreshold = thresholdParsed;
                        SendMessage(chatId, $"Длина периода Мао успешно установлена. Теперь Мао будет говорить раз в {threshold} слов.");
                        return;
                    }
                    catch(Exception ex)
                    {
                        Log("В SetMessageThreshold передано корректное значение {0}, но что-то пошло не так.", threshold);
                        SendMessage(chatId, $"Не получилось назначить настройке значение {threshold}. Попробуйте другое.");
                    }
                }
                Log("В SetMessageThreshold передано некорректное значение {0}! Отвечаю грубостью.", threshold);
                SendMessage(chatId, $"Кто-то глупенький)) {threshold} — не натуральное число. Попробуй ещё раз.");
            }
            else
            {
                Log("По идее недостижимый код, чата {0} нет в списке чатов в момент смены настройки.", chatId);
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
                        Chats[chatId].QuotationsFileName = availableQuotationFiles[fileNumberParsed];

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
            sb.AppendLine($"Word Count: {WordCount}");
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
            sb.AppendLine($"Message Threshold: {MessageThreshold}");
            sb.Append($"QuotationsFileName: {QuotationsFileName}");
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
    }
}
