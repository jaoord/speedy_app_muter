using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.Linq;

namespace SpeedyAppMuter.Services
{
    public class AudioSessionManager : IDisposable
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMDevice? _defaultDevice;
        private bool _disposed = false;

        public AudioSessionManager()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        /// <summary>
        /// Toggles the mute state for all audio sessions belonging to the specified process names
        /// </summary>
        /// <param name="processNames">Array of process names to target (e.g., "firefox", "firefox.exe")</param>
        /// <returns>True if any sessions were found and toggled, false otherwise</returns>
        public bool ToggleMuteForProcess(string[] processNames)
        {
            if (_defaultDevice?.AudioSessionManager?.Sessions == null)
                return false;

            bool anySessionToggled = false;
            
            // Normalize process names (remove .exe extension for comparison)
            var normalizedNames = processNames
                .Select(name => name.ToLower().Replace(".exe", ""))
                .ToArray();

            try
            {
                for (int i = 0; i < _defaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    var session = _defaultDevice.AudioSessionManager.Sessions[i];
                    
                    // Skip system sessions
                    if (session.GetProcessID == 0)
                        continue;

                    try
                    {
                        // Get the process for this audio session
                        var process = Process.GetProcessById((int)session.GetProcessID);
                        var processName = process.ProcessName.ToLower();

                        // Check if this process matches any of our target process names
                        if (normalizedNames.Contains(processName))
                        {
                            // Toggle the mute state
                            var currentMuteState = session.SimpleAudioVolume.Mute;
                            session.SimpleAudioVolume.Mute = !currentMuteState;
                            
                            anySessionToggled = true;
                            
                            Debug.WriteLine($"Toggled mute for {process.ProcessName} (PID: {process.Id}) - Now {(session.SimpleAudioVolume.Mute ? "muted" : "unmuted")}");
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists, skip
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing audio session: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating audio sessions: {ex.Message}");
                return false;
            }

            return anySessionToggled;
        }

        /// <summary>
        /// Gets the current mute state for the specified process names
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>True if any matching process is currently muted, false otherwise</returns>
        public bool IsProcessMuted(string[] processNames)
        {
            if (_defaultDevice?.AudioSessionManager?.Sessions == null)
                return false;

            var normalizedNames = processNames
                .Select(name => name.ToLower().Replace(".exe", ""))
                .ToArray();

            try
            {
                for (int i = 0; i < _defaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    var session = _defaultDevice.AudioSessionManager.Sessions[i];
                    
                    if (session.GetProcessID == 0)
                        continue;

                    try
                    {
                        var process = Process.GetProcessById((int)session.GetProcessID);
                        var processName = process.ProcessName.ToLower();

                        if (normalizedNames.Contains(processName))
                        {
                            if (session.SimpleAudioVolume.Mute)
                                return true;
                        }
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking mute state: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Checks if any of the specified processes are currently running
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <returns>True if any matching process is running, false otherwise</returns>
        public bool IsProcessRunning(string[] processNames)
        {
            var normalizedNames = processNames
                .Select(name => name.ToLower().Replace(".exe", ""))
                .ToArray();

            try
            {
                var runningProcesses = Process.GetProcesses()
                    .Select(p => p.ProcessName.ToLower())
                    .ToArray();

                return normalizedNames.Any(name => runningProcesses.Contains(name));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking running processes: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _defaultDevice?.Dispose();
                _deviceEnumerator?.Dispose();
                _disposed = true;
            }
        }
    }
} 