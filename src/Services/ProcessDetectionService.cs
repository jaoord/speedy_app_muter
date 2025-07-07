using NAudio.CoreAudioApi;
using SpeedyAppMuter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SpeedyAppMuter.Services
{
    public class ProcessDetectionService : IDisposable
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMDevice? _defaultDevice;
        private bool _disposed = false;

        public ProcessDetectionService()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        /// <summary>
        /// Gets all running processes that have active audio sessions
        /// </summary>
        /// <returns>List of processes with audio sessions</returns>
        public List<ProcessInfo> GetProcessesWithAudio()
        {
            var processesWithAudio = new List<ProcessInfo>();

            if (_defaultDevice?.AudioSessionManager?.Sessions == null)
                return processesWithAudio;

            var seenProcesses = new HashSet<int>();

            try
            {
                for (int i = 0; i < _defaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    var session = _defaultDevice.AudioSessionManager.Sessions[i];
                    
                    // Skip system sessions
                    if (session.GetProcessID == 0)
                        continue;

                    var processId = (int)session.GetProcessID;
                    
                    // Skip if we've already processed this process
                    if (seenProcesses.Contains(processId))
                        continue;

                    seenProcesses.Add(processId);

                    try
                    {
                        var process = Process.GetProcessById(processId);
                        var processInfo = CreateProcessInfo(process, true);
                        
                        if (processInfo != null)
                        {
                            processesWithAudio.Add(processInfo);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists, skip
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting process info for PID {processId}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating audio sessions: {ex.Message}");
            }

            return processesWithAudio.OrderBy(p => p.DisplayName).ToList();
        }

        /// <summary>
        /// Gets all running processes (whether they have audio or not)
        /// </summary>
        /// <returns>List of all running processes</returns>
        public List<ProcessInfo> GetAllRunningProcesses()
        {
            var allProcesses = new List<ProcessInfo>();
            var processesWithAudio = GetProcessesWithAudio().ToDictionary(p => p.ProcessId);

            try
            {
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        // Skip system processes and processes without main window
                        if (string.IsNullOrEmpty(process.ProcessName) || 
                            process.ProcessName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool hasAudio = processesWithAudio.ContainsKey(process.Id);
                        var processInfo = CreateProcessInfo(process, hasAudio);
                        
                        if (processInfo != null)
                        {
                            allProcesses.Add(processInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing {process.ProcessName}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all processes: {ex.Message}");
            }

            return allProcesses.OrderBy(p => p.DisplayName).ToList();
        }

        /// <summary>
        /// Creates a ProcessInfo object from a Process
        /// </summary>
        private ProcessInfo? CreateProcessInfo(Process process, bool hasAudio)
        {
            try
            {
                var processInfo = new ProcessInfo
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    HasAudioSession = hasAudio
                };

                // Try to get executable path and friendly name
                try
                {
                    processInfo.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                    
                    // Create display name
                    if (!string.IsNullOrEmpty(processInfo.ExecutablePath))
                    {
                        var fileInfo = new FileInfo(processInfo.ExecutablePath);
                        var versionInfo = FileVersionInfo.GetVersionInfo(processInfo.ExecutablePath);
                        
                        // Use product name if available, otherwise use filename without extension
                        processInfo.DisplayName = !string.IsNullOrEmpty(versionInfo.ProductName) 
                            ? versionInfo.ProductName 
                            : fileInfo.Name.Replace(fileInfo.Extension, "");
                    }
                    else
                    {
                        processInfo.DisplayName = process.ProcessName;
                    }
                }
                catch
                {
                    // Fallback to process name if we can't get file info
                    processInfo.DisplayName = process.ProcessName;
                }

                // Add audio indicator to display name
                if (hasAudio)
                {
                    processInfo.DisplayName += " 🔊";
                }

                return processInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating ProcessInfo for {process.ProcessName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets friendly names for common applications
        /// </summary>
        public static Dictionary<string, string> GetCommonAppNames()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "firefox", "Mozilla Firefox" },
                { "chrome", "Google Chrome" },
                { "msedge", "Microsoft Edge" },
                { "brave", "Brave Browser" },
                { "opera", "Opera" },
                { "spotify", "Spotify" },
                { "discord", "Discord" },
                { "vlc", "VLC Media Player" },
                { "wmplayer", "Windows Media Player" },
                { "winamp", "Winamp" },
                { "steam", "Steam" },
                { "skype", "Skype" },
                { "zoom", "Zoom" },
                { "teams", "Microsoft Teams" },
                { "slack", "Slack" }
            };
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