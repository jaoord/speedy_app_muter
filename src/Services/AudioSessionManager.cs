using NAudio.CoreAudioApi;
using SpeedyAppMuter.Utils;
using System;
using System.Diagnostics;
using System.Linq;

namespace SpeedyAppMuter.Services
{
    public class AudioSessionManager : IDisposable
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMDevice? _defaultDevice;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public AudioSessionManager(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        /// <summary>
        /// Toggles the mute state for all audio sessions belonging to the specified process names
        /// </summary>
        /// <param name="processNames">Array of process names to target (e.g., "firefox", "firefox.exe")</param>
        /// <returns>Result indicating success and whether any sessions were toggled</returns>
        public Result<bool> ToggleMuteForProcess(string[] processNames)
        {
            try
            {
                bool anySessionToggled = false;
                var processSessionPairs = ProcessSessionHelper.GetProcessSessions(_defaultDevice, processNames);
                
                foreach (var (session, process) in processSessionPairs)
                {
                    try
                    {
                        // Toggle the mute state
                        var currentMuteState = session.SimpleAudioVolume.Mute;
                        session.SimpleAudioVolume.Mute = !currentMuteState;
                        
                        anySessionToggled = true;
                        
                        _logger.LogInfo($"Toggled mute for {process.ProcessName} (PID: {process.Id}) - Now {(session.SimpleAudioVolume.Mute ? "muted" : "unmuted")}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error toggling mute for {process.ProcessName}", ex);
                        continue;
                    }
                }

                return Result<bool>.Success(anySessionToggled);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ToggleMuteForProcess", ex);
                return Result<bool>.Failure(ex);
            }
        }

        /// <summary>
        /// Gets the current mute state for the specified process names
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>Result indicating success and whether any matching process is muted</returns>
        public Result<bool> IsProcessMuted(string[] processNames)
        {
            try
            {
                var processSessionPairs = ProcessSessionHelper.GetProcessSessions(_defaultDevice, processNames);
                
                foreach (var (session, process) in processSessionPairs)
                {
                    try
                    {
                        if (session.SimpleAudioVolume.Mute)
                            return Result<bool>.Success(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error checking mute state for {process.ProcessName}", ex);
                        continue;
                    }
                }

                return Result<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in IsProcessMuted", ex);
                return Result<bool>.Failure(ex);
            }
        }

        /// <summary>
        /// Checks if any of the specified processes are currently running
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>Result indicating success and whether any matching process is running</returns>
        public Result<bool> IsProcessRunning(string[] processNames)
        {
            try
            {
                var isRunning = ProcessSessionHelper.IsAnyProcessRunning(processNames);
                return Result<bool>.Success(isRunning);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking if process is running", ex);
                return Result<bool>.Failure(ex);
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility - toggles mute and returns simple boolean
        /// </summary>
        /// <param name="processNames">Array of process names to target</param>
        /// <returns>True if any sessions were found and toggled, false otherwise</returns>
        [Obsolete("Use ToggleMuteForProcess that returns Result<bool> instead")]
        public bool ToggleMuteForProcessLegacy(string[] processNames)
        {
            return ToggleMuteForProcess(processNames).GetValueOrDefault(false);
        }

        /// <summary>
        /// Legacy method for backward compatibility - checks mute state and returns simple boolean
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>True if any matching process is currently muted, false otherwise</returns>
        [Obsolete("Use IsProcessMuted that returns Result<bool> instead")]
        public bool IsProcessMutedLegacy(string[] processNames)
        {
            return IsProcessMuted(processNames).GetValueOrDefault(false);
        }

        /// <summary>
        /// Legacy method for backward compatibility - checks if processes are running and returns simple boolean
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>True if any matching process is running, false otherwise</returns>
        [Obsolete("Use IsProcessRunning that returns Result<bool> instead")]
        public bool IsProcessRunningLegacy(string[] processNames)
        {
            return IsProcessRunning(processNames).GetValueOrDefault(false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _defaultDevice?.Dispose();
                _deviceEnumerator?.Dispose();
                _disposed = true;
                _logger.LogDebug("AudioSessionManager disposed");
            }

            GC.SuppressFinalize(this);
        }
    }
} 