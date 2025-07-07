using System;

namespace SpeedyAppMuter.Models
{
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public bool HasAudioSession { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProcessInfo other && ProcessName.Equals(other.ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return ProcessName.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
} 