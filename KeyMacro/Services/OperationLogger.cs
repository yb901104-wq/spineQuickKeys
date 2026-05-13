namespace KeyMacro.Services;

public static class OperationLogger
{
    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro", "logs");
    private static readonly object _lock = new();
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MaxAgeDays = 7;

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                CleanupOldFiles();

                var logFile = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

                if (File.Exists(logFile) && new FileInfo(logFile).Length >= MaxFileSize)
                {
                    int suffix = 1;
                    string rotated;
                    do
                    {
                        rotated = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.{suffix}.log");
                        suffix++;
                    } while (File.Exists(rotated));
                    File.Move(logFile, rotated);
                }

                File.AppendAllText(logFile, line + Environment.NewLine);
            }
            catch
            {
                // Logger must never throw
            }
        }
    }

    private static void CleanupOldFiles()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-MaxAgeDays);
            foreach (var file in Directory.GetFiles(LogDir, "*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch { }
    }
}
