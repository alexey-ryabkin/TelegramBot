using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot.Types;
using static TelegramBot.Logger;
using static TelegramBot.QuotationsFinder;

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
        internal string QuotationsFileName {  get; set; }
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
                Log("Чат {0} добавлен в словарь обслуживаемых чатов.", ChatId);
            }
        }
        [JsonConstructor]
        public ChatInfo(long ChatId, 
            int WordCount, 
            Dictionary<string, int> Words, 
            Dictionary<int, string[]> Messages, 
            int MessageThreshold, 
            string QuotationsFileName)
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
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(Chats.Values, options);
            File.WriteAllText(fileName, jsonString);
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
            return sb.ToString();
        }
    }
}
