using SpeedyAppMuter.Models;
using SpeedyAppMuter.Utils;
using System;

namespace SpeedyAppMuter.Services
{
    /// <summary>
    /// Core application controller that handles business logic
    /// Separated from UI concerns to follow Single Responsibility Principle
    /// </summary>
    public class ApplicationController : IDisposable
    {
        private readonly AudioSessionManager _audioManager;
        private readonly HotkeyManager _hotkeyManager;
        private readonly ConfigurationService _configService;
        private readonly ILogger _logger;
        private AppConfig _config;
        private bool _disposed = false;

        public event EventHandler? HotkeyPressed;
        public event EventHandler<AppConfig>? ConfigurationChanged;

        public ApplicationController(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            _configService = new ConfigurationService();
            _audioManager = new AudioSessionManager(_logger);
            _hotkeyManager = new HotkeyManager(_logger);

            // Load and validate initial configuration
            _config = _configService.LoadConfiguration();
            _configService.ValidateConfiguration(_config);

            // Set up hotkey handling
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            
            RegisterHotkey();
        }

        /// <summary>
        /// Gets the current application configuration
        /// </summary>
        public AppConfig Configuration => _config;

        /// <summary>
        /// Toggles the mute state for the target application
        /// </summary>
        /// <returns>Result indicating success and whether any sessions were toggled</returns>
        public Result<bool> ToggleMute()
        {
            try
            {
                var result = _audioManager.ToggleMuteForProcess(_config.TargetApplication.ProcessNames);
                
                if (result.IsSuccess)
                {
                    if (result.Value)
                    {
                        var muteResult = _audioManager.IsProcessMuted(_config.TargetApplication.ProcessNames);
                        bool isMuted = muteResult.GetValueOrDefault(false);
                        _logger.LogInfo($"Audio toggled - {_config.TargetApplication.Name} is now {(isMuted ? "muted" : "unmuted")}");
                    }
                    else
                    {
                        _logger.LogInfo($"No audio sessions found for {_config.TargetApplication.Name}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error toggling mute", ex);
                return Result<bool>.Failure(ex);
            }
        }

        /// <summary>
        /// Checks if the target process is currently muted
        /// </summary>
        /// <returns>Result indicating success and mute state</returns>
        public Result<bool> IsProcessMuted()
        {
            return _audioManager.IsProcessMuted(_config.TargetApplication.ProcessNames);
        }

        /// <summary>
        /// Checks if the target process is currently running
        /// </summary>
        /// <returns>Result indicating success and running state</returns>
        public Result<bool> IsProcessRunning()
        {
            return _audioManager.IsProcessRunning(_config.TargetApplication.ProcessNames);
        }

        /// <summary>
        /// Reloads the configuration from file
        /// </summary>
        /// <returns>Result indicating success</returns>
        public Result ReloadConfiguration()
        {
            try
            {
                _hotkeyManager.UnregisterHotkey();

                _config = _configService.LoadConfiguration();
                _configService.ValidateConfiguration(_config);

                RegisterHotkey();

                _logger.LogInfo("Configuration reloaded successfully");
                ConfigurationChanged?.Invoke(this, _config);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reloading configuration", ex);
                return Result.Failure(ex);
            }
        }

        /// <summary>
        /// Updates the configuration with new settings
        /// </summary>
        /// <param name="newConfig">The new configuration to apply</param>
        /// <returns>Result indicating success</returns>
        public Result ApplyConfiguration(AppConfig newConfig)
        {
            try
            {
                _hotkeyManager.UnregisterHotkey();

                _config = newConfig;

                RegisterHotkey();

                _logger.LogInfo("Configuration updated successfully");
                ConfigurationChanged?.Invoke(this, _config);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying configuration", ex);
                return Result.Failure(ex);
            }
        }

        /// <summary>
        /// Gets the status information for the current configuration
        /// </summary>
        /// <returns>Status information including running and mute states</returns>
        public (bool IsRunning, bool IsMuted) GetStatus()
        {
            var runningResult = IsProcessRunning();
            var isRunning = runningResult.GetValueOrDefault(false);
            
            var mutedResult = isRunning ? IsProcessMuted() : Result<bool>.Success(false);
            var isMuted = mutedResult.GetValueOrDefault(false);

            return (isRunning, isMuted);
        }

        private void RegisterHotkey()
        {
            try
            {
                bool success = _hotkeyManager.RegisterHotkey(_config.TargetApplication.Hotkey);
                if (success)
                {
                    _logger.LogInfo("Hotkey registered successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to register hotkey");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error registering hotkey", ex);
            }
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _hotkeyManager?.Dispose();
                _audioManager?.Dispose();
                _disposed = true;
                _logger.LogDebug("ApplicationController disposed");
            }

            GC.SuppressFinalize(this);
        }
    }
} 