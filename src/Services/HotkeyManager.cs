using SpeedyAppMuter.Models;
using SpeedyAppMuter.Utils;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SpeedyAppMuter.Services
{
    public class HotkeyManager : IDisposable
    {
        private readonly Form _messageWindow;
        private int _hotkeyId = 1;
        private bool _disposed = false;

        public event EventHandler? HotkeyPressed;

        public HotkeyManager()
        {
            // Create an invisible form to receive Windows messages
            _messageWindow = new Form()
            {
                WindowState = FormWindowState.Minimized,
                ShowInTaskbar = false,
                Visible = false
            };
            
            // Override the WndProc to handle hotkey messages
            _messageWindow.Load += (s, e) => _messageWindow.Hide();
        }

        /// <summary>
        /// Registers a global hotkey based on the configuration
        /// </summary>
        /// <param name="hotkeyConfig">The hotkey configuration from the JSON file</param>
        /// <returns>True if the hotkey was successfully registered, false otherwise</returns>
        public bool RegisterHotkey(HotkeyConfig hotkeyConfig)
        {
            if (_disposed)
                return false;

            try
            {
                // Convert configuration to Win32 values
                var modifiers = Win32Helpers.GetModifierFlags(hotkeyConfig.Modifiers);
                var keyCode = Win32Helpers.GetVirtualKeyCode(hotkeyConfig.Key);

                if (keyCode == 0)
                {
                    Debug.WriteLine($"Invalid key: {hotkeyConfig.Key}");
                    return false;
                }

                // Register the hotkey
                bool success = Win32Helpers.RegisterHotKey(
                    _messageWindow.Handle,
                    _hotkeyId,
                    modifiers,
                    keyCode
                );

                if (success)
                {
                    Debug.WriteLine($"Registered hotkey: {string.Join("+", hotkeyConfig.Modifiers)}+{hotkeyConfig.Key}");
                    
                    // Set up message handling
                    _messageWindow.KeyDown += OnKeyDown;
                    Application.AddMessageFilter(new HotkeyMessageFilter(_hotkeyId, OnHotkeyPressed));
                }
                else
                {
                    Debug.WriteLine($"Failed to register hotkey: {string.Join("+", hotkeyConfig.Modifiers)}+{hotkeyConfig.Key}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering hotkey: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregisters the currently registered hotkey
        /// </summary>
        public void UnregisterHotkey()
        {
            if (!_disposed && _messageWindow.Handle != IntPtr.Zero)
            {
                Win32Helpers.UnregisterHotKey(_messageWindow.Handle, _hotkeyId);
                Debug.WriteLine("Unregistered hotkey");
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // This might not be needed, but keeping for potential debugging
        }

        private void OnHotkeyPressed()
        {
            Debug.WriteLine("Hotkey pressed!");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterHotkey();
                _messageWindow?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Message filter to handle WM_HOTKEY messages
    /// </summary>
    internal class HotkeyMessageFilter : IMessageFilter
    {
        private readonly int _hotkeyId;
        private readonly Action _onHotkeyPressed;

        public HotkeyMessageFilter(int hotkeyId, Action onHotkeyPressed)
        {
            _hotkeyId = hotkeyId;
            _onHotkeyPressed = onHotkeyPressed;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == Win32Helpers.WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId)
            {
                _onHotkeyPressed?.Invoke();
                return true;
            }
            return false;
        }
    }
} 