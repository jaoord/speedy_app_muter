using System;

namespace SpeedyAppMuter.Utils
{
    /// <summary>
    /// Interface for logging operations throughout the application
    /// Provides consistent logging methods to replace scattered Debug.WriteLine calls
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="exception">Optional exception details</param>
        void LogError(string message, Exception? exception = null);

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The informational message</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a debug message (only in debug builds)
        /// </summary>
        /// <param name="message">The debug message</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        void LogWarning(string message);
    }

    /// <summary>
    /// Default logger implementation using Debug.WriteLine
    /// Can be easily replaced with more sophisticated logging frameworks
    /// </summary>
    public class DebugLogger : ILogger
    {
        private readonly string _prefix;

        public DebugLogger(string prefix = "")
        {
            _prefix = string.IsNullOrEmpty(prefix) ? "" : $"[{prefix}] ";
        }

        public void LogError(string message, Exception? exception = null)
        {
            var fullMessage = $"{_prefix}ERROR: {message}";
            if (exception != null)
            {
                fullMessage += $" - {exception.Message}";
            }
            System.Diagnostics.Debug.WriteLine(fullMessage);
        }

        public void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"{_prefix}INFO: {message}");
        }

        public void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"{_prefix}DEBUG: {message}");
        }

        public void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"{_prefix}WARNING: {message}");
        }
    }

    /// <summary>
    /// Static logger instance for easy access throughout the application
    /// </summary>
    public static class Logger
    {
        private static ILogger _instance = new DebugLogger("SpeedyAppMuter");

        /// <summary>
        /// Gets the current logger instance
        /// </summary>
        public static ILogger Instance => _instance;

        /// <summary>
        /// Sets a custom logger implementation
        /// </summary>
        /// <param name="logger">The logger implementation to use</param>
        public static void SetLogger(ILogger logger)
        {
            _instance = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Convenience method for logging errors
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="exception">Optional exception details</param>
        public static void Error(string message, Exception? exception = null)
        {
            _instance.LogError(message, exception);
        }

        /// <summary>
        /// Convenience method for logging information
        /// </summary>
        /// <param name="message">The informational message</param>
        public static void Info(string message)
        {
            _instance.LogInfo(message);
        }

        /// <summary>
        /// Convenience method for logging debug messages
        /// </summary>
        /// <param name="message">The debug message</param>
        public static void Debug(string message)
        {
            _instance.LogDebug(message);
        }

        /// <summary>
        /// Convenience method for logging warnings
        /// </summary>
        /// <param name="message">The warning message</param>
        public static void Warning(string message)
        {
            _instance.LogWarning(message);
        }
    }
} 