using static TelegramBot.Logger;

namespace TelegramBot
{
    /// <summary>
    /// Чат{Сумма слов, словарь частоты появления слов, словарь ID сообщения+список слов}
    /// </summary>
    /// message_id, chat
    internal class ChatInfo
    {
        // Словарь чатов, в которых работает бот
        internal static Dictionary<long, ChatInfo> Chats { get; set; } = new();
        // long по требованию API, 64 бита
        internal long chatId { get; init; }
        // текущее набранное количество слов
        internal int WordCount { get; set; } = 0;
        // словарь частоты появления слов
        internal Dictionary<string, int> Words { get; set; } = new();
        // message_id, список слов в этом сообщении
        // слово — слово длинее 4 букв в нижнем регистре
        internal Dictionary<int, string[]> Messages { get; set; } = new();
        internal MaoFollower maoFollower { get; set; } = new();
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
                maoFollower = new MaoFollower();
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
