using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static TelegramBot.Logger;

namespace TelegramBot
{
    /// <summary>
    /// Одна из цитат в коллекции цитат.
    /// </summary>
    internal class Quotation
    {
        [JsonInclude]
        internal string author;
        [JsonInclude]
        internal string source;
        [JsonInclude]
        internal string text;
        public Quotation(string author, string source, string text)
        {
            this.author = author;
            this.source = source;
            this.text = text;
        }
        public override string ToString()
        {
            string result;
            if (source.Length == 0)
            {
                result = $"{text}\n\n{author}.";
            }
            else
            {
                result = $"{text}\n\n{author}. {(source[source.Length - 1] == '.' ? source : source + ".")}";
            }
            
            return result;
        }
    }

    /// <summary>
    /// Коллекция цитат (база данных в памяти).  
    /// Этот класс реализует IEnumerable, чтобы его можно было использовать с синтаксисом LINQ.
    /// https://learn.microsoft.com/ru-ru/dotnet/api/system.collections.ienumerable?view=net-8.0
    /// </summary>
    internal class Quotations : IEnumerable
    {
        private Quotation[] _quotations;
        /// <summary>
        /// Создаёт набор цитат с заданными цитатами.
        /// </summary>
        /// <param name="_quotations">Список цитат, которые нужно добавить в набор при создании.</param>
        internal Quotations(Quotation[] _quotations)
        {
            int length = _quotations.Length;
            this._quotations = new Quotation[length];
            for (int i = 0; i < length; i++) this._quotations[i] = _quotations[i];
        }
        /// <summary>
        /// Создаёт пустой набор цитат.
        /// </summary>
        internal Quotations()
        {
            _quotations = Array.Empty<Quotation>();
        }
        internal void Add(Quotation quotation)
        {
            int length = _quotations.Length;
            Quotation[] newQuotations = new Quotation[length + 1];
            for (int i = 0; i < length; i++) newQuotations[i] = _quotations[i];
            newQuotations[length] = quotation;
            _quotations = newQuotations;
        }
        /// <summary>
        /// Создаёт экземпляр списка цитат из файла JSON по указанному пути.
        /// </summary>
        /// <param name="fileName">Название файла JSON или путь к нему.</param>
        /// <returns>Экземпляр базы данных цитат.</returns>
        internal static Quotations LoadFromJSON(string fileName)
        {
            Quotations quotationsDB = new Quotations();
            try
            {
                string jsonString = File.ReadAllText(fileName);
                List<Quotation> quotationsList = JsonSerializer.Deserialize<List<Quotation>>(jsonString)!;
                quotationsDB = new Quotations(quotationsList.ToArray());
                Log("Цитаты успешно загружены из файла {0}.", fileName);
            }
            catch
            {
                Log("Загрузка базы цитат из файла {0} не удалась.", fileName);
            }
            return quotationsDB;
        }
        /// <summary>
        /// Сохраняет экземпляр базы данных цитат в файл JSON по указанному пути с индентацией и без экранирования. Существующий файл будет перезаписан.
        /// </summary>
        /// <param name="fileName">Путь, название файла.</param>
        internal void SerializeToJSON(string fileName)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(fileName, jsonString);
        }

        // Implementation for the GetEnumerator method.  
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        internal QuotationsEnum GetEnumerator()
        {
            return new QuotationsEnum(_quotations);
        }
        public int Count()
        {
            return _quotations.Length;
        }
        public override string ToString()
        {
            string result = string.Empty;
            result = string.Join("\n" + new string('-', 50) + "\n", (IEnumerable<Quotation>)_quotations);
            return result;
        }
    }

    // When you implement IEnumerable, you must also implement IEnumerator.  
    internal class QuotationsEnum : IEnumerator
    {
        private Quotation[] _quotations;

        // Enumerators are positioned before the first element  
        // until the first MoveNext() call.  
        int position = -1;

        public QuotationsEnum(Quotation[] _quotations)
        {
            this._quotations = _quotations;
        }

        public bool MoveNext()
        {
            position++;
            return (position < _quotations.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public Quotation Current
        {
            get
            {
                try
                {
                    return _quotations[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
