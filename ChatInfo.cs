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
        /// Словарь чатов, в которых работает бот. Ключ — chatId, значение — экземпляр класса ChatInfo.
        /// </summary>
        internal static Dictionary<long, ChatInfo> Chats { get; set; } = new();
        /// <summary>
        /// Идентификатор чата, тип значения long по требованию библиотеки.
        /// </summary>
        internal long chatId { get; init; }
        /// <summary>
        /// Количество слов, которые были обработаны в чате.
        /// </summary>
        internal int WordCount { get; set; } = 0;
        /// <summary>
        /// Словарь, в котором ключ — слово, а значение — количество раз, когда это слово встречалось в чате.
        /// </summary>
        internal Dictionary<string, int> Words { get; set; } = new();
        /// <summary>
        /// Словарь, в котором ключ — идентификатор сообщения, а значение — массив слов, которые были найдены в этом сообщении.
        /// Слово — слово длинее 4 букв в нижнем регистре.
        /// </summary>
        internal Dictionary<int, string[]> Messages { get; set; } = new();
        /// <summary>
        /// Экземпляр класса QuotationsFinder, который ищет цитаты в загруженной для него базе цитат.
        /// </summary>
        internal QuotationsFinder quotationsFinder { get; set; }
        internal int MessageThreshold { get; set; } = 100;
        public ChatInfo(long chatId)
        {
            if (Chats.ContainsKey(chatId))
            {
                Log("Чат {0} уже есть в словаре обслуживаемых чатов. Программа работает некорректно.", chatId);
                throw new ApplicationException("Чат уже есть в словаре обслуживаемых чатов.");
            }
            else
            {
                this.chatId = chatId;
                Chats.Add(chatId, this);
                quotationsFinder = GetQuotationsFinder();
                Log("Чат {0} добавлен в словарь обслуживаемых чатов.", chatId);
            }
        }
        public void AddMessage(int messageId, string[] words)
        {
            if (Messages.ContainsKey(messageId))
            {
                Log("Сообщение {0} для чата {1} уже есть в словаре. Пропускаю.", messageId, chatId);
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
                    //Log("Слово {0} встречается {1} раз в чате {2}", pair.Key, pair.Value, chatId);
                }
            }
        }

    }
}
