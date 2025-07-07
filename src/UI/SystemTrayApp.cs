using SpeedyAppMuter.Models;
using SpeedyAppMuter.Services;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SpeedyAppMuter.UI
{
    public class SystemTrayApp : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ConfigurationService _configService;
        private readonly AudioSessionManager _audioManager;
        private readonly HotkeyManager _hotkeyManager;
        private AppConfig _config;
        private bool _disposed = false;

        public SystemTrayApp()
        {
            _configService = new ConfigurationService();
            _audioManager = new AudioSessionManager();
            _hotkeyManager = new HotkeyManager();

            _config = _configService.LoadConfiguration();
            _configService.ValidateConfiguration(_config);

            _contextMenu = CreateContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = $"Speedy App Muter - {_config.TargetApplication.Name}",
                Visible = _config.Settings.ShowTrayIcon,
                ContextMenuStrip = _contextMenu
            };

            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;

            RegisterHotkey();

            Debug.WriteLine("System tray application initialized successfully");
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var toggleMuteItem = new ToolStripMenuItem("Toggle Mute")
            {
                Font = new Font(menu.Font, FontStyle.Bold)
            };
            toggleMuteItem.Click += (s, e) => ToggleMute();

            var statusItem = new ToolStripMenuItem("Status: Ready")
            {
                Enabled = false
            };

            var separator1 = new ToolStripSeparator();

            var appInfoItem = new ToolStripMenuItem($"Target: {_config.TargetApplication.Name}")
            {
                Enabled = false
            };

            var hotkeyInfoItem = new ToolStripMenuItem($"Hotkey: {string.Join("+", _config.TargetApplication.Hotkey.Modifiers)}+{_config.TargetApplication.Hotkey.Key}")
            {
                Enabled = false
            };

            var separator2 = new ToolStripSeparator();

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (s, e) => OpenSettings();

            var openConfigItem = new ToolStripMenuItem("Open Config File");
            openConfigItem.Click += (s, e) => OpenConfigFile();

            var reloadConfigItem = new ToolStripMenuItem("Reload Config");
            reloadConfigItem.Click += (s, e) => ReloadConfiguration();

            var separator3 = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();

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

        private Icon CreateIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(Brushes.Blue, 2, 2, 12, 12);
                graphics.DrawString("M", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 4, 2);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void RegisterHotkey()
        {
            try
            {
                bool success = _hotkeyManager.RegisterHotkey(_config.TargetApplication.Hotkey);
                if (success)
                {
                    Debug.WriteLine("Hotkey registered successfully");
                    UpdateTrayTooltip();
                }
                else
                {
                    Debug.WriteLine("Failed to register hotkey");
                    _notifyIcon.ShowBalloonTip(3000, "Hotkey Registration Failed", 
                        $"Could not register hotkey: {string.Join("+", _config.TargetApplication.Hotkey.Modifiers)}+{_config.TargetApplication.Hotkey.Key}", 
                        ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering hotkey: {ex.Message}");
            }
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            ToggleMute();
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            ToggleMute();
        }

        private void ToggleMute()
        {
            try
            {
                bool success = _audioManager.ToggleMuteForProcess(_config.TargetApplication.ProcessNames);
                
                if (success)
                {
                    bool isMuted = _audioManager.IsProcessMuted(_config.TargetApplication.ProcessNames);
                    Debug.WriteLine($"Audio toggled - {_config.TargetApplication.Name} is now {(isMuted ? "muted" : "unmuted")}");
                    UpdateTrayIcon(isMuted);
                }
                else
                {
                    Debug.WriteLine($"No audio sessions found for {_config.TargetApplication.Name}");
                    UpdateTrayIcon(false);
                }

                UpdateContextMenuStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling mute: {ex.Message}");
            }
        }

        private void UpdateTrayIcon(bool isMuted)
        {
            // Update icon color based on mute state
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                var brush = isMuted ? Brushes.Red : Brushes.Blue;
                graphics.FillEllipse(brush, 2, 2, 12, 12);
                graphics.DrawString("M", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 4, 2);
            }
            
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
        }

        private void UpdateTrayTooltip()
        {
            bool isRunning = _audioManager.IsProcessRunning(_config.TargetApplication.ProcessNames);
            bool isMuted = isRunning ? _audioManager.IsProcessMuted(_config.TargetApplication.ProcessNames) : false;
            
            string status = isRunning ? (isMuted ? "Muted" : "Running") : "Not Running";
            _notifyIcon.Text = $"Speedy App Muter - {_config.TargetApplication.Name} ({status})";
        }

        private void UpdateContextMenuStatus()
        {
            if (_contextMenu.Items.Count > 1 && _contextMenu.Items[1] is ToolStripMenuItem statusItem)
            {
                bool isRunning = _audioManager.IsProcessRunning(_config.TargetApplication.ProcessNames);
                bool isMuted = isRunning ? _audioManager.IsProcessMuted(_config.TargetApplication.ProcessNames) : false;
                
                string status = isRunning ? (isMuted ? "Muted" : "Running") : "Not Running";
                statusItem.Text = $"Status: {status}";
            }
        }

        private void OpenConfigFile()
        {
            try
            {
                string configPath = _configService.GetConfigFilePath();
                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening config file: {ex.Message}");
                _notifyIcon.ShowBalloonTip(3000, "Error", "Could not open config file", ToolTipIcon.Error);
            }
        }

        private void ReloadConfiguration()
        {
            try
            {
                _hotkeyManager.UnregisterHotkey();

                _config = _configService.LoadConfiguration();
                _configService.ValidateConfiguration(_config);

                RegisterHotkey();

                UpdateTrayTooltip();
                UpdateContextMenuInfo();

                Debug.WriteLine("Configuration reloaded successfully");
                _notifyIcon.ShowBalloonTip(2000, "Configuration Reloaded", 
                    $"Target: {_config.TargetApplication.Name}\nHotkey: {string.Join("+", _config.TargetApplication.Hotkey.Modifiers)}+{_config.TargetApplication.Hotkey.Key}", 
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reloading configuration: {ex.Message}");
                _notifyIcon.ShowBalloonTip(3000, "Error", "Could not reload configuration", ToolTipIcon.Error);
            }
        }

        private void OpenSettings()
        {
            try
            {
                Debug.WriteLine("Opening settings window...");
                
                var configCopy = new AppConfig
                {
                    TargetApplication = new TargetApplication
                    {
                        Name = _config.TargetApplication.Name,
                        ProcessNames = _config.TargetApplication.ProcessNames,
                        Hotkey = new HotkeyConfig
                        {
                            Key = _config.TargetApplication.Hotkey.Key,
                            Modifiers = _config.TargetApplication.Hotkey.Modifiers
                        }
                    },
                    Settings = new AppSettings()
                };

                var settingsForm = new SettingsForm(configCopy);
                settingsForm.ConfigurationChanged += OnSettingsChanged;
                
                Debug.WriteLine("Showing settings dialog...");
                var result = settingsForm.ShowDialog();
                Debug.WriteLine($"Settings dialog result: {result}");
                
                if (result == DialogResult.OK)
                {
                    Debug.WriteLine("Settings saved successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening settings: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _notifyIcon.ShowBalloonTip(3000, "Error", $"Could not open settings window: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void OnSettingsChanged(object? sender, AppConfig newConfig)
        {
            try
            {
                _hotkeyManager.UnregisterHotkey();

                _config = newConfig;

                RegisterHotkey();

                UpdateTrayTooltip();
                UpdateContextMenuInfo();

                Debug.WriteLine("Configuration updated from settings window");
                _notifyIcon.ShowBalloonTip(2000, "Settings Applied", 
                    $"Target: {_config.TargetApplication.Name}\nHotkey: {string.Join("+", _config.TargetApplication.Hotkey.Modifiers)}+{_config.TargetApplication.Hotkey.Key}", 
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying settings: {ex.Message}");
                _notifyIcon.ShowBalloonTip(3000, "Error", "Could not apply settings", ToolTipIcon.Error);
            }
        }

        private void UpdateContextMenuInfo()
        {
            if (_contextMenu.Items.Count > 4)
            {
                if (_contextMenu.Items[3] is ToolStripMenuItem appInfoItem)
                {
                    appInfoItem.Text = $"Target: {_config.TargetApplication.Name}";
                }
                
                if (_contextMenu.Items[4] is ToolStripMenuItem hotkeyInfoItem)
                {
                    hotkeyInfoItem.Text = $"Hotkey: {string.Join("+", _config.TargetApplication.Hotkey.Modifiers)}+{_config.TargetApplication.Hotkey.Key}";
                }
            }
        }

        public void Run()
        {
            if (_config.Settings.StartMinimized)
            {
                Debug.WriteLine("Starting minimized to system tray");
            }

            Application.Run();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _hotkeyManager?.Dispose();
                _audioManager?.Dispose();
                _contextMenu?.Dispose();
                _notifyIcon?.Dispose();
                _disposed = true;
            }
        }
    }
} 