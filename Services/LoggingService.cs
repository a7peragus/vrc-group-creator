using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VrcGroupCreator.Services;

public static class LoggingService
{
    [DllImport("kernel32.dll")] private static extern bool AllocConsole();
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();

    private static readonly string LogFolder;
    private static readonly string CurrentLogFile;
    private static readonly object _lock = new();
    private static StreamWriter? _logWriter;

    public static bool DebugEnabled { get; private set; }

    static LoggingService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VrcGroupCreator");

        LogFolder = Path.Combine(appData, "Logs");
        Directory.CreateDirectory(LogFolder);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        CurrentLogFile = Path.Combine(LogFolder, $"log_{timestamp}.txt");

        // Keep last 10 logs
        CleanupOldLogs();
    }

    public static bool FileLoggingEnabled { get; private set; }

    public static void Initialize(bool enableFileLogging = true)
    {
        FileLoggingEnabled = enableFileLogging;
        if (!FileLoggingEnabled) return;

        try
        {
            _logWriter = new StreamWriter(CurrentLogFile, append: true) { AutoFlush = true };
            Info("LOG", "Logging initialized");
            Info("LOG", $"Log file: {CurrentLogFile}");
            Info("LOG", $"OS: {Environment.OSVersion}");
            Info("LOG", $".NET: {Environment.Version}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to initialize log file: {ex.Message}");
        }
    }

    public static void SetFileLoggingEnabled(bool enable)
    {
        if (FileLoggingEnabled == enable) return;
        FileLoggingEnabled = enable;

        lock (_lock)
        {
            if (enable)
            {
                try
                {
                    _logWriter = new StreamWriter(CurrentLogFile, append: true) { AutoFlush = true };
                    Info("LOG", "File logging enabled.");
                }
                catch { }
            }
            else
            {
                Info("LOG", "File logging disabled.");
                _logWriter?.Flush();
                _logWriter?.Dispose();
                _logWriter = null;
            }
        }
    }

    public static void SetConsoleEnabled(bool enable)
    {
        DebugEnabled = enable;

        if (enable)
        {
            AllocConsole();

            // Re-bind stdout so Console.WriteLine actually works in WPF
            var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            var stdErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetOut(stdOut);
            Console.SetError(stdErr);

            Info("LOG", "Debug console attached — API requests will be logged here.");
        }
        else
        {
            Info("LOG", "Detaching debug console.");
            FreeConsole();
        }
    }

    public static void Log(string level, string source, string message,
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        // Strip API keys from logs to prevent leaking secrets in debug console or files
        if (!string.IsNullOrEmpty(message))
        {
            message = System.Text.RegularExpressions.Regex.Replace(message, @"apiKey=[A-Za-z0-9]+", "apiKey=***");
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var consoleLine = $"[{timestamp}] [{level,-5}] [{source}] {message}";
        var fileLine    = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] [{source}] {message}";

        if (DebugEnabled)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = level == "ERROR" ? ConsoleColor.Red : ConsoleColor.White;
            Console.WriteLine(consoleLine);
            Console.ForegroundColor = prev;
        }

        lock (_lock)
        {
            try { _logWriter?.WriteLine(fileLine); }
            catch { }
        }
    }

    public static void Info (string source, string message) => Log("INFO",  source, message);
    public static void Debug(string source, string message) => Log("DEBUG", source, message);
    public static void Warn (string source, string message) => Log("WARN",  source, message);
    public static void Error(string source, string message) => Log("ERROR", source, message);

    public static void Error(string source, Exception ex, string context = "")
    {
        var msg = string.IsNullOrEmpty(context)
            ? $"{ex.GetType().Name}: {ex.Message}"
            : $"{context} — {ex.GetType().Name}: {ex.Message}";
        Log("ERROR", source, msg);
        Log("ERROR", source, $"Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
            Log("ERROR", source, $"Inner: {ex.InnerException.Message}");
    }

    /// <summary>Log an outgoing HTTP request.</summary>
    public static void ApiRequest(string method, string? uri)
        => Log("API", "HTTP", $">> {method} {uri}");

    /// <summary>Log an incoming HTTP response, truncating large bodies.</summary>
    public static void ApiResponse(string method, string? uri, int status, string body)
    {
        var preview = body.Length > 800 ? body[..800] + " …(truncated)" : body;
        Log("API", "HTTP", $"<< {status} {method} {uri}");
        Log("API", "HTTP", $"   Body: {preview}");
    }

    public static void Shutdown()
    {
        Info("LOG", "Application shutting down.");
        lock (_lock)
        {
            _logWriter?.Flush();
            _logWriter?.Dispose();
            _logWriter = null;
        }
    }

    public static string GetLogFolder() => LogFolder;
    public static string GetCurrentLogFile() => CurrentLogFile;

    private static void CleanupOldLogs()
    {
        try
        {
            var files = Directory.GetFiles(LogFolder, "log_*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(10);
            foreach (var f in files)
                try { f.Delete(); } catch { }
        }
        catch { }
    }
}
