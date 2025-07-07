using System.Text.Json.Serialization;

namespace SpeedyAppMuter.Models
{
    public class AppConfig
    {
        [JsonPropertyName("targetApplication")]
        public TargetApplication TargetApplication { get; set; } = new();

        [JsonPropertyName("settings")]
        public AppSettings Settings { get; set; } = new();
    }

    public class TargetApplication
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Firefox";

        [JsonPropertyName("processNames")]
        public string[] ProcessNames { get; set; } = ["firefox", "firefox.exe"];

        [JsonPropertyName("hotkey")]
        public HotkeyConfig Hotkey { get; set; } = new();
    }

    public class HotkeyConfig
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "F9";

        [JsonPropertyName("modifiers")]
        public string[] Modifiers { get; set; } = ["Ctrl", "Alt"];
    }

    public class AppSettings
    {
        [JsonPropertyName("startMinimized")]
        public bool StartMinimized { get; } = true;

        [JsonPropertyName("showTrayIcon")]
        public bool ShowTrayIcon { get; } = true;

        [JsonPropertyName("windowState")]
        public WindowState WindowState { get; set; } = new();
    }

    public class WindowState
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 650;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 480;

        [JsonPropertyName("x")]
        public int X { get; set; } = -1; // -1 means center on screen

        [JsonPropertyName("y")]
        public int Y { get; set; } = -1; // -1 means center on screen

        [JsonPropertyName("isMaximized")]
        public bool IsMaximized { get; set; } = false;
    }
} 