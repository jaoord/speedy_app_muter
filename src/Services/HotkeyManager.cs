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
        private readonly ILogger _logger;
        private int _hotkeyId = Constants.Performance.DefaultHotkeyId;
        private HotkeyMessageFilter? _currentMessageFilter;
        private bool _disposed = false;

        public event EventHandler? HotkeyPressed;

        public HotkeyManager(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            
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
                    _logger.LogWarning($"Invalid key: {hotkeyConfig.Key}");
                    return false;
                }

                // Unregister any existing hotkey first (this also removes the message filter)
                UnregisterHotkey();

                // Register the new hotkey
                bool success = Win32Helpers.RegisterHotKey(_messageWindow.Handle, _hotkeyId, modifiers, keyCode);

                if (success)
                {
                    // Create and add new message filter to handle WM_HOTKEY messages
                    _currentMessageFilter = new HotkeyMessageFilter(_hotkeyId, () => HotkeyPressed?.Invoke(this, EventArgs.Empty));
                    Application.AddMessageFilter(_currentMessageFilter);
                    _logger.LogInfo($"Registered hotkey: {string.Join("+", hotkeyConfig.Modifiers)}+{hotkeyConfig.Key}");
                }
                else
                {
                    _logger.LogWarning($"Failed to register hotkey: {string.Join("+", hotkeyConfig.Modifiers)}+{hotkeyConfig.Key}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error registering hotkey", ex);
                return false;
            }
        }

        /// <summary>
        /// Unregisters the currently registered hotkey
        /// </summary>
        public void UnregisterHotkey()
        {
            if (_disposed)
                return;

            try
            {
                // Remove the message filter if it exists
                if (_currentMessageFilter != null)
                {
                    Application.RemoveMessageFilter(_currentMessageFilter);
                    _currentMessageFilter = null;
                    _logger.LogDebug("Removed message filter");
                }

                // Unregister the Win32 hotkey
                Win32Helpers.UnregisterHotKey(_messageWindow.Handle, _hotkeyId);
                _logger.LogDebug("Unregistered hotkey");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error unregistering hotkey", ex);
            }
        }

        /// <summary>
        /// Simulates a hotkey press for testing purposes
        /// </summary>
        public void SimulateHotkeyPress()
        {
            _logger.LogDebug("Hotkey pressed!");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterHotkey();
                _messageWindow?.Dispose();
                _disposed = true;
                _logger.LogDebug("HotkeyManager disposed");
            }

            GC.SuppressFinalize(this);
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