using KaimiraGames;

namespace TelegramBot
{
    ///
    /// | word    | nextWord
    /// |         | i | want | to | eat | chinese | food |
    /// |---------|---|------|----|-----|---------|------|
    /// | i       | 0 | 1    | 0  | 0   | 0       | 0    |
    /// | want    | 0 | 0    | 1  | 0   | 0       | 0    |
    /// | to      | 0 | 0    | 0  | 1   | 0       | 0    |
    /// | eat     | 0 | 0    | 0  | 0   | 1       | 0    |
    /// | chinese | 0 | 0    | 0  | 0   | 0       | 1    |
    /// | food    | 0 | 0    | 0  | 0   | 0       | 0    |
    /// Ключи внешних и внутренних словарей всегда одинаковы.
    /// 
    /// Внешний словарь:
    /// i    = | i | want | to | eat | chinese | food |
    /// want = | i | want | to | eat | chinese | food |
    /// 
    /// Внутренний словарь:
    /// | i | want | to | eat | chinese | food |
    /// |---|------|----|-----|---------|------|
    /// | 0 | 1    | 0  | 0   | 0       | 0    |
    /// <summary>
    /// Класс, реализующий цепь Маркова.
    /// </summary>
    public class MarkovChain
    {
        private Dictionary<string, Dictionary<string, int>> _wordMatrix;
        // Два параметра для корректной печати
        private Dictionary<string, int> maxValueLength;
        private int maxKeyLength;
        public MarkovChain()
        {
            _wordMatrix = new Dictionary<string, Dictionary<string, int>>();
            // Длина самого длинного значения в столбце
            maxValueLength = new Dictionary<string, int>();
            maxKeyLength = 0;
        }
        public void AddText(string text)
        {
            AddText(text.Split());
        }
        public void AddText(string[] words)
        {
            if (words.Length < 2) return;
            for (int i = 1; i < words.Length; i++)
            {
                //Stopwatch stopwatch = Stopwatch.StartNew();
                _add(words[i - 1], words[i]);
                //stopwatch.Stop();
                //Console.WriteLine("{2, 10} тиков: {0} {1}", words[i - 1], words[i], stopwatch.ElapsedTicks);
            }
        }
        /// <summary>
        /// Добавляет во все словари новый ключ с нулевым значением.
        /// </summary>
        /// <param name="word"></param>
        private void _addKey(string word)
        {
            Dictionary<string, int> innerDictionaryIter, newWordDictionary;
            if (!_wordMatrix.ContainsKey(word))
            {
                // Новый внутренний словарь
                newWordDictionary = new Dictionary<string, int>();
                // За словом, которое мы ранее не видели, пока что ничего не следует.
                foreach (string key in _wordMatrix.Keys)
                {
                    newWordDictionary.Add(key, 0);
                }
                _wordMatrix.Add(word, newWordDictionary);
                // Ни в одном из внутренних словарей нет ключа newWord, добавляю
                // Иначе, синхронизирую ключи наружного словаря и ключи внутренних словарей
                foreach (string key in _wordMatrix.Keys)
                {
                    innerDictionaryIter = _wordMatrix[key];
                    innerDictionaryIter.Add(word, 0);
                }
                // Обновление значений, записанных для форматирования вывода на печать
                int l = word.Length;
                maxValueLength[word] = l;
                if (maxKeyLength < l) maxKeyLength = l;
            }
        }
        private void _add(string word, string nextWord)
        {
            // Первое слово в тексте, поэтому первого слова может не быть в словаре.
            _addKey(word);
            _addKey(nextWord);
            // Теперь точно есть все необходимые ключи, можно использовать +=
            _wordMatrix[word][nextWord]++;
            // Обновление длины самого длинного значения в столбце
            int newValueLength = _wordMatrix[word][nextWord].ToString().Length;
            if (maxValueLength[nextWord] < newValueLength) maxValueLength[nextWord] = newValueLength;
        }
        /// <summary>
        /// Возвращает взвешенный список, который используется один раз для определения следующего слова с учётом коэффициентов из текущей базы данных
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private WeightedList<string> _GetWeighedList(string word)
        {
            if (_wordMatrix.ContainsKey(word))
            {
                WeightedList<string> wl = new WeightedList<string>();
                wl.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
                foreach (string key in _wordMatrix[word].Keys)
                {
                    int weight = _wordMatrix[word][key];
                    if (weight > 0) wl.Add(key, weight);
                }
                // Если у поданного слова нет вариантов последующих (оно последнее в тексте), то нужно выбрать случайное.
                if (wl.Count == 0) return _GetWeighedList();
                return wl;
            }
            else
            {
                // Если в качестве параметра передано неизвестное слово, то нужно выбрать случайное с равной вероятностью.
                return _GetWeighedList();
            }
        }
        private WeightedList<string> _GetWeighedList()
        {
            // Если в качестве параметра не подано слово, то нужно выбрать случайное с равной вероятностью.
            string[] keys = _wordMatrix.Keys.ToArray();
            WeightedListItem<string>[] wli = new WeightedListItem<string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                wli[i] = new WeightedListItem<string>(keys[i], 1);
            }
            WeightedList<string> wl = new WeightedList<string>(wli);
            wl.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
            return wl;
        }
        private string _next(string word)
        {
            if (_wordMatrix.Count < 1)
            {
                throw new ApplicationException("Невозможно выбрать следующее слово, так как нет исходных данных");
            }
            return _GetWeighedList(word).Next();
        }
        private string _next()
        {
            if (_wordMatrix.Count < 1)
            {
                throw new ApplicationException("Невозможно выбрать следующее слово, так как нет исходных данных");
            }
            return _GetWeighedList().Next();
        }
        public string CreateText(int size)
        {
            string result = string.Empty;
            string word;
            word = _next();
            result += word;
            for (int i = 0; i < size; i++)
            {
                word = _next(word);
                result += " " + word;
                //Console.WriteLine("Слово {0} из {1}.", i, size);
            }
            return result;
        }
        public string CreateText()
        {
            return CreateText(50);
        }
        public override string ToString()
        {
            string result = string.Empty;
            // Отсортированные ключи, заголовки строк и столбцов
            string[] keys = _wordMatrix.Keys.ToList().Order().ToArray();
            string format;
            int l = keys.Length;
            int maxValueLength;
            result += "|" + new string(' ', maxKeyLength) + "|" + string.Join("|", keys) + "|" + Environment.NewLine;
            for (int i = 0; i < l; i++)
            {
                format = $"|{{0,{maxKeyLength}}}";
                result += string.Format(format, keys[i]);
                for (int j = 0; j < l; j++)
                {
                    maxValueLength = this.maxValueLength[keys[j]];
                    format = $"|{{0,{maxValueLength}}}";
                    result += string.Format(format, _wordMatrix[keys[i]][keys[j]]);
                    Console.WriteLine("Строка {0}, столбец {1} из {2}", i, j, l);
                }
                result += string.Format("|" + Environment.NewLine);
            }
            return result;
        }
    }
}
