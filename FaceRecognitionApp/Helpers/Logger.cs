using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FaceRecognitionApp.Helpers;

/// <summary>
/// Centralized logging helper for consistent debug output
/// </summary>
public static class AppLogger
{
    private const string Prefix = "FaceRecognitionApp";

    public static void Info(string message, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [INFO] {caller}: {message}");
#endif
    }

    public static void Success(string message, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [ OK ] {caller}: {message}");
#endif
    }

    public static void Warning(string message, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [WARN] {caller}: {message}");
#endif
    }

    public static void Error(string message, Exception? exception = null, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [ERR ] {caller}: {message}");
        if (exception != null)
        {
            Debug.WriteLine($"[{Prefix}]    Exception: {exception.GetType().Name} - {exception.Message}");
        }
#endif
    }

    public static void Error(Exception exception, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [ERR ] {caller}: {exception.GetType().Name} - {exception.Message}");
        Debug.WriteLine($"[{Prefix}]    StackTrace: {exception.StackTrace}");
#endif
    }

    public static void Trace(string message, [CallerMemberName] string? caller = null)
    {
#if DEBUG
        Debug.WriteLine($"[{Prefix}] [TRC] {caller}: {message}");
#endif
    }
}
