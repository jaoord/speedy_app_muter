using SpeedyAppMuter.Models;
using SpeedyAppMuter.Services;
using SpeedyAppMuter.Utils;
using System;
using System.Windows.Forms;

namespace SpeedyAppMuter.UI
{
    /// <summary>
    /// Main system tray application coordinator
    /// Now focused on coordinating between UI and business logic components
    /// </summary>
    public class SystemTrayApp : IDisposable
    {
        private readonly ApplicationController _controller;
        private readonly TrayIconManager _trayManager;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public SystemTrayApp(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            
            // Initialize the business logic controller
            _controller = new ApplicationController(_logger);
            
            // Initialize the tray icon manager
            _trayManager = new TrayIconManager(_controller.Configuration, _logger);

            // Wire up events
            _controller.HotkeyPressed += OnHotkeyPressed;
            _controller.ConfigurationChanged += OnConfigurationChanged;
            
            _trayManager.ToggleMuteRequested += OnToggleMuteRequested;
            _trayManager.OpenSettingsRequested += OnOpenSettingsRequested;
            _trayManager.ReloadConfigRequested += OnReloadConfigRequested;
            _trayManager.ExitRequested += OnExitRequested;

            // Update initial UI state
            UpdateUI();

            _logger.LogInfo("System tray application initialized successfully");
        }

        public void Run()
        {
            if (_controller.Configuration.Settings.StartMinimized)
            {
                _logger.LogInfo("Starting minimized to system tray");
            }

            Application.Run();
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            ToggleMute();
        }

        private void OnToggleMuteRequested(object? sender, EventArgs e)
        {
            ToggleMute();
        }

        private void ToggleMute()
        {
            var result = _controller.ToggleMute();
            
            result.OnFailure(error => 
            {
                _trayManager.ShowBalloonTip(Constants.UI.BalloonTipTimeout, Constants.Messages.ErrorTitle, 
                    "Failed to toggle mute", ToolTipIcon.Error);
            });

            UpdateUI();
        }

        private void OnOpenSettingsRequested(object? sender, EventArgs e)
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            try
            {
                _logger.LogDebug("Opening settings window...");
                
                var configCopy = CreateConfigCopy(_controller.Configuration);
                var settingsForm = new SettingsForm(configCopy);
                settingsForm.ConfigurationChanged += OnSettingsChanged;
                
                _logger.LogDebug("Showing settings dialog...");
                var result = settingsForm.ShowDialog();
                _logger.LogDebug($"Settings dialog result: {result}");
                
                if (result == DialogResult.OK)
                {
                    _logger.LogInfo("Settings saved successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening settings", ex);
                _trayManager.ShowBalloonTip(Constants.UI.BalloonTipTimeout, Constants.Messages.ErrorTitle, 
                    $"Could not open settings window: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void OnSettingsChanged(object? sender, AppConfig newConfig)
        {
            var result = _controller.ApplyConfiguration(newConfig);
            
            result.OnSuccess(() =>
            {
                _trayManager.ShowBalloonTip(Constants.UI.SuccessBalloonTipTimeout, Constants.Messages.SettingsAppliedTitle, 
                    $"Target: {newConfig.TargetApplication.Name}\nHotkey: {string.Join("+", newConfig.TargetApplication.Hotkey.Modifiers)}+{newConfig.TargetApplication.Hotkey.Key}", 
                    ToolTipIcon.Info);
            })
            .OnFailure(error =>
            {
                _trayManager.ShowBalloonTip(Constants.UI.BalloonTipTimeout, Constants.Messages.ErrorTitle, 
                    "Could not apply settings", ToolTipIcon.Error);
            });

            UpdateUI();
        }

        private void OnReloadConfigRequested(object? sender, EventArgs e)
        {
            var result = _controller.ReloadConfiguration();
            
            result.OnSuccess(() =>
            {
                var config = _controller.Configuration;
                _trayManager.ShowBalloonTip(Constants.UI.SuccessBalloonTipTimeout, Constants.Messages.ConfigurationReloadedTitle, 
                    $"Target: {config.TargetApplication.Name}\nHotkey: {string.Join("+", config.TargetApplication.Hotkey.Modifiers)}+{config.TargetApplication.Hotkey.Key}", 
                    ToolTipIcon.Info);
            })
            .OnFailure(error =>
            {
                _trayManager.ShowBalloonTip(Constants.UI.BalloonTipTimeout, Constants.Messages.ErrorTitle, 
                    "Could not reload configuration", ToolTipIcon.Error);
            });

            UpdateUI();
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnConfigurationChanged(object? sender, AppConfig newConfig)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var (isRunning, isMuted) = _controller.GetStatus();
            var config = _controller.Configuration;

            _trayManager.UpdateIcon(isMuted);
            _trayManager.UpdateTooltip(config, isRunning, isMuted);
            _trayManager.UpdateContextMenu(config, isRunning, isMuted);
        }

        private AppConfig CreateConfigCopy(AppConfig original)
        {
            return new AppConfig
            {
                TargetApplication = new TargetApplication
                {
                    Name = original.TargetApplication.Name,
                    ProcessNames = original.TargetApplication.ProcessNames,
                    Hotkey = new HotkeyConfig
                    {
                        Key = original.TargetApplication.Hotkey.Key,
                        Modifiers = original.TargetApplication.Hotkey.Modifiers
                    }
                },
                Settings = new AppSettings()
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _controller?.Dispose();
                _trayManager?.Dispose();
                _disposed = true;
                _logger.LogDebug("SystemTrayApp disposed");
            }

            GC.SuppressFinalize(this);
        }
    }
} 