using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeedyAppMuter.Utils
{
    public static class Win32Helpers
    {
        // Hotkey registration constants
        public const int WM_HOTKEY = 0x0312;
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        // Win32 API declarations
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        /// <summary>
        /// Converts string modifier names to Win32 modifier flags
        /// </summary>
        public static uint GetModifierFlags(string[] modifiers)
        {
            uint flags = 0;
            foreach (var modifier in modifiers)
            {
                flags |= modifier.ToLower() switch
                {
                    "ctrl" or "control" => (uint)MOD_CONTROL,
                    "alt" => (uint)MOD_ALT,
                    "shift" => (uint)MOD_SHIFT,
                    "win" or "windows" => (uint)MOD_WIN,
                    _ => 0u
                };
            }
            return flags;
        }

        /// <summary>
        /// Converts string key name to virtual key code
        /// </summary>
        public static uint GetVirtualKeyCode(string key)
        {
            // Handle function keys
            if (key.StartsWith("F") && int.TryParse(key[1..], out int fNum) && fNum >= 1 && fNum <= 24)
            {
                return (uint)(Keys.F1 + fNum - 1);
            }

            // Handle common keys
            return key.ToUpper() switch
            {
                "A" => (uint)Keys.A,
                "B" => (uint)Keys.B,
                "C" => (uint)Keys.C,
                "D" => (uint)Keys.D,
                "E" => (uint)Keys.E,
                "F" => (uint)Keys.F,
                "G" => (uint)Keys.G,
                "H" => (uint)Keys.H,
                "I" => (uint)Keys.I,
                "J" => (uint)Keys.J,
                "K" => (uint)Keys.K,
                "L" => (uint)Keys.L,
                "M" => (uint)Keys.M,
                "N" => (uint)Keys.N,
                "O" => (uint)Keys.O,
                "P" => (uint)Keys.P,
                "Q" => (uint)Keys.Q,
                "R" => (uint)Keys.R,
                "S" => (uint)Keys.S,
                "T" => (uint)Keys.T,
                "U" => (uint)Keys.U,
                "V" => (uint)Keys.V,
                "W" => (uint)Keys.W,
                "X" => (uint)Keys.X,
                "Y" => (uint)Keys.Y,
                "Z" => (uint)Keys.Z,
                "SPACE" => (uint)Keys.Space,
                "ENTER" => (uint)Keys.Enter,
                "ESC" or "ESCAPE" => (uint)Keys.Escape,
                _ => 0
            };
        }
    }
} 