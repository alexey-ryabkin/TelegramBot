namespace TelegramBot
{
    internal static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TelegramBot",
            "log.txt"
        );

        private static readonly object _lock = new object();

        static Logger()
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания директории: {ex.Message}");
            }
        }
        public static void Log(string message, params object[] args)
        {
            lock (_lock)
            {
                try
                {
                    string logEntry = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {string.Format(message, args)}";
                    Console.WriteLine(logEntry);
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка записи лога: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Логирует сообщение, если программа запущена с ключом verbose. Используется для отладки и тестирования.
        /// </summary>
        public static void LogVerbose(string message, params object[] args)
        {
            if (TelegramBot.optionsVerbose) Log(message, args);
        }
    }
}