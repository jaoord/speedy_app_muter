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
        public bool StartMinimized { get; set; } = true;

        [JsonPropertyName("showTrayIcon")]
        public bool ShowTrayIcon { get; set; } = true;
    }
} 