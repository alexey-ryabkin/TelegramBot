using static TelegramBot.Logger;
namespace TelegramBot
{
    enum SearchStatus: byte
    {
        Found = 0,
        NotFound = 1,
        NoSource = 2
        //Error = 3
    }
    internal readonly record struct SearchResult(SearchStatus status,
                                                 string word,
                                                 string quotation,
                                                 string source,
                                                 string author);

    internal class MaoFollower
    {
        static Random rng = new Random();
        internal Quotations quotationsDB;
        internal MaoFollower()
        {
            quotationsDB = Quotations.LoadFromJSON("quotations.json");
        }
        internal MaoFollower(string filepath)
        {
            quotationsDB = Quotations.LoadFromJSON(filepath);
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