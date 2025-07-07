using SpeedyAppMuter.Models;
using SpeedyAppMuter.Utils;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SpeedyAppMuter.UI
{
    /// <summary>
    /// Manages the system tray icon and context menu
    /// Separated from SystemTrayApp to follow Single Responsibility Principle
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public event EventHandler? ToggleMuteRequested;
        public event EventHandler? OpenSettingsRequested;
        public event EventHandler? ReloadConfigRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager(AppConfig config, ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            _contextMenu = CreateContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = IconFactory.CreateDefaultIcon(),
                Text = $"{Constants.Application.ApplicationName} - {config.TargetApplication.Name}",
                Visible = config.Settings.ShowTrayIcon,
                ContextMenuStrip = _contextMenu
            };

            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        /// <summary>
        /// Updates the tray icon based on mute state
        /// </summary>
        /// <param name="isMuted">True if the process is muted</param>
        public void UpdateIcon(bool isMuted)
        {
            // Dispose old icon safely
            IconFactory.SafeDisposeIcon(_notifyIcon.Icon);
            
            // Create new icon based on mute state
            _notifyIcon.Icon = IconFactory.CreateMuteIcon(isMuted);
        }

        /// <summary>
        /// Updates the tooltip text
        /// </summary>
        /// <param name="config">Current application configuration</param>
        /// <param name="isRunning">Whether the target process is running</param>
        /// <param name="isMuted">Whether the target process is muted</param>
        public void UpdateTooltip(AppConfig config, bool isRunning, bool isMuted)
        {
            string status = isRunning ? (isMuted ? "Muted" : "Running") : "Not Running";
            _notifyIcon.Text = $"{Constants.Application.ApplicationName} - {config.TargetApplication.Name} ({status})";
        }

        /// <summary>
        /// Updates the context menu with current configuration info
        /// </summary>
        /// <param name="config">Current application configuration</param>
        /// <param name="isRunning">Whether the target process is running</param>
        /// <param name="isMuted">Whether the target process is muted</param>
        public void UpdateContextMenu(AppConfig config, bool isRunning, bool isMuted)
        {
            if (_contextMenu.Items.Count > 1 && _contextMenu.Items[1] is ToolStripMenuItem statusItem)
            {
                string status = isRunning ? (isMuted ? "Muted" : "Running") : "Not Running";
                statusItem.Text = $"Status: {status}";
            }

            if (_contextMenu.Items.Count > 4)
            {
                if (_contextMenu.Items[3] is ToolStripMenuItem appInfoItem)
                {
                    appInfoItem.Text = $"Target: {config.TargetApplication.Name}";
                }
                
                if (_contextMenu.Items[4] is ToolStripMenuItem hotkeyInfoItem)
                {
                    hotkeyInfoItem.Text = $"Hotkey: {string.Join("+", config.TargetApplication.Hotkey.Modifiers)}+{config.TargetApplication.Hotkey.Key}";
                }
            }
        }

        /// <summary>
        /// Shows a balloon tip notification
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="title">Notification title</param>
        /// <param name="text">Notification text</param>
        /// <param name="icon">Notification icon type</param>
        public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        /// <summary>
        /// Sets the visibility of the tray icon
        /// </summary>
        /// <param name="visible">True to show the icon, false to hide it</param>
        public void SetVisible(bool visible)
        {
            _notifyIcon.Visible = visible;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var toggleMuteItem = new ToolStripMenuItem("Toggle Mute")
            {
                Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold)
            };
            toggleMuteItem.Click += (s, e) => ToggleMuteRequested?.Invoke(this, EventArgs.Empty);

            var statusItem = new ToolStripMenuItem("Status: Unknown")
            {
                Enabled = false
            };

            var separator1 = new ToolStripSeparator();

            var appInfoItem = new ToolStripMenuItem("Target: Unknown")
            {
                Enabled = false
            };

            var hotkeyInfoItem = new ToolStripMenuItem("Hotkey: Unknown")
            {
                Enabled = false
            };

            var separator2 = new ToolStripSeparator();

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

            var openConfigItem = new ToolStripMenuItem("Open Config File...");
            openConfigItem.Click += OnOpenConfigFile;

            var reloadConfigItem = new ToolStripMenuItem("Reload Configuration");
            reloadConfigItem.Click += (s, e) => ReloadConfigRequested?.Invoke(this, EventArgs.Empty);

            var separator3 = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.AddRange(new ToolStripItem[]
            {
                toggleMuteItem,
                statusItem,
                separator1,
                appInfoItem,
                hotkeyInfoItem,
                separator2,
                settingsItem,
                openConfigItem,
                reloadConfigItem,
                separator3,
                exitItem
            });

            return menu;
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            ToggleMuteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnOpenConfigFile(object? sender, EventArgs e)
        {
            try
            {
                var configService = new Services.ConfigurationService();
                string configPath = configService.GetConfigFilePath();
                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening config file", ex);
                ShowBalloonTip(Constants.UI.BalloonTipTimeout, Constants.Messages.ErrorTitle, "Could not open config file", ToolTipIcon.Error);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _contextMenu?.Dispose();
                
                // Safely dispose the icon
                IconFactory.SafeDisposeIcon(_notifyIcon?.Icon);
                _notifyIcon?.Dispose();
                
                _disposed = true;
                _logger.LogDebug("TrayIconManager disposed");
            }
            
            GC.SuppressFinalize(this);
        }
    }
} 