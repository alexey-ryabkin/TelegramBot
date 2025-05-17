using static TelegramBot.Logger;
namespace TelegramBot
{
    enum SearchStatus: byte
    {
        Found = 0,
        NotFound = 1,
        NoSource = 2
    }
    internal readonly record struct SearchResult(SearchStatus status,
                                                 string word,
                                                 string quotation,
                                                 string source,
                                                 string author);
    /// <summary>
    /// Класс, который ищет цитаты Мао Цзэдуна в базе данных QuotationsDB.
    /// </summary>
    internal class QuotationsFinder
    {
        internal static Random rng = new Random();
        internal static Dictionary<string, QuotationsFinder> QuotationsFinders;
        private static string[] availableQuotationFiles = 
        { 
            "quotations.json"
        };
        internal Quotations quotationsDB;
        internal string fileName;
        /// <summary>
        /// Статический конструктор загружает в память все допустимые наборы цитат.
        /// </summary>
        static QuotationsFinder()
        {
            QuotationsFinders = new Dictionary<string, QuotationsFinder>();
            foreach (string file in availableQuotationFiles)
            {
                QuotationsFinders.Add(file, new QuotationsFinder(file));
            }
        }
        private QuotationsFinder(string filepath)
        {
            quotationsDB = Quotations.LoadFromJSON(filepath);
            fileName = filepath;
        }
        internal static QuotationsFinder GetQuotationsFinder()
        {
            return GetQuotationsFinder(availableQuotationFiles[0]);
        }
        /// <summary>
        /// Метод, который нужно использовать вместо конструктора для экономии места в памяти.
        /// </summary>
        /// <param name="filepath">Название файла с цитатами</param>
        /// <returns></returns>
        internal static QuotationsFinder GetQuotationsFinder(string filepath)
        {
            if (QuotationsFinders.ContainsKey(filepath))
            {
                return QuotationsFinders[filepath];
            }
            else
            {
                Log("Не удалось найти файл {0} в словаре QuotationsFinders. Возвращаю цитаты по умолчанию.", filepath);
                return QuotationsFinders[availableQuotationFiles[0]];
            }
        }
        /// <summary>
        /// Ищет в базе данных quotationsDB текущего экземпляра цитату, 
        /// которая содержит одно из слов из переданного словаря.
        /// В приоритете более редкие слова.
        /// Если слово не найдено, возвращает результат с соответствующим значеним статуса.
        /// </summary>
        /// <param name="words">Словарь, в котором ключи — слова, а значения — количество этих слов в тексте.</param>
        /// <returns>Экземпляр структуры, описывающей результаты поиска.</returns>
        internal SearchResult Search(Dictionary<string, int> words)
        {
            bool wordFound = false;
            int numberOfResults;
            string currentWord;
            string currentWordShortened;
            SearchResult searchResult = new SearchResult() { status = SearchStatus.NotFound };
            Quotation quotation;
            IEnumerable<KeyValuePair<string, int>> wordQuery;
            IEnumerable<Quotation> quotationQuery;
            IEnumerator<KeyValuePair<string, int>> wordsEnum;

            // Если не были загружены цитаты Мао.
            if (quotationsDB.Count() == 0)
            {
                Log("В цитатах Мао не удалось найти ни одно слово, так как программа не загрузила цитаты в память.");
                return new SearchResult() { status = SearchStatus.NoSource };
            }

            wordQuery = from word in words
                        orderby word.Value ascending
                        select word;
            wordsEnum = wordQuery.GetEnumerator();

            while (wordsEnum.MoveNext() && !wordFound)
            {
                currentWord = wordsEnum.Current.Key;
                currentWordShortened = currentWord[..^1];
                quotationQuery = from Quotation quotationInQuery in quotationsDB
                                 where quotationInQuery.text.Contains(currentWordShortened)
                                 select quotationInQuery;
                numberOfResults = quotationQuery.Count();
                Log("Найдено {0} цитат, содержащих слово {1}.", numberOfResults, currentWord);
                if (numberOfResults > 0)
                {
                    wordFound = true;
                    quotation = quotationQuery.ElementAt(rng.Next(0, numberOfResults));
                    searchResult = new SearchResult() { status = SearchStatus.Found, 
                        author = quotation.author, 
                        source = quotation.source, 
                        quotation = quotation.text, 
                        word = currentWord};
                }
            }
            switch (searchResult.status)
            {
                case (SearchStatus.NotFound):
                    Log("В цитатах Мао не удалось найти ни одно слово из переданного словаря.");
                    break;
                case (SearchStatus.Found):
                    Log("В цитатах Мао найдено слово! {0}", searchResult.word);
                    break;
            }
            return searchResult;
        }
    }
}