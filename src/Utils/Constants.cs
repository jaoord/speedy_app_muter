using System.Drawing;

namespace SpeedyAppMuter.Utils
{
    /// <summary>
    /// Centralized constants for the application
    /// Eliminates magic numbers and hard-coded values throughout the codebase
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// UI-related constants
        /// </summary>
        public static class UI
        {
            public const int TrayIconSize = 16;
            public const int DefaultWindowWidth = 650;
            public const int DefaultWindowHeight = 480;
            public const int BalloonTipTimeout = 3000;
            public const int SuccessBalloonTipTimeout = 2000;
            
            // Icon positioning within the tray icon
            public const int IconCircleX = 2;
            public const int IconCircleY = 2;
            public const int IconCircleSize = 12;
            public const int IconTextX = 4;
            public const int IconTextY = 2;
            public const int IconFontSize = 8;
            public const string IconFontFamily = "Arial";
            public const string IconText = "M";
        }

        /// <summary>
        /// Color constants
        /// </summary>
        public static class Colors
        {
            public static readonly Color MutedColor = Color.Red;
            public static readonly Color UnmutedColor = Color.Blue;
            public static readonly Color IconTextColor = Color.White;
            public static readonly Color TransparentColor = Color.Transparent;
        }

        /// <summary>
        /// Application constants
        /// </summary>
        public static class Application
        {
            public const string MutexName = "SpeedyAppMuter_SingleInstance";
            public const string DefaultConfigFileName = "config.json";
            public const string ApplicationName = "Speedy App Muter";
        }

        /// <summary>
        /// Audio session constants
        /// </summary>
        public static class Audio
        {
            public const uint SystemProcessId = 0;
            public const string ExeExtension = ".exe";
        }

        /// <summary>
        /// Performance constants
        /// </summary>
        public static class Performance
        {
            public const int ProcessCacheExpirationSeconds = 5;
            public const int DefaultHotkeyId = 1;
        }

        /// <summary>
        /// Message constants
        /// </summary>
        public static class Messages
        {
            public const string AlreadyRunningTitle = "Already Running";
            public const string AlreadyRunningMessage = "Speedy App Muter is already running. Check your system tray.";
            public const string FatalErrorTitle = "Fatal Error";
            public const string ErrorTitle = "Error";
            public const string HotkeyRegistrationFailedTitle = "Hotkey Registration Failed";
            public const string ConfigurationReloadedTitle = "Configuration Reloaded";
            public const string SettingsAppliedTitle = "Settings Applied";
        }
    }
} 