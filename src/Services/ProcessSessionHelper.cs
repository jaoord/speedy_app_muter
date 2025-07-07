using NAudio.CoreAudioApi;
using SpeedyAppMuter.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpeedyAppMuter.Services
{
    /// <summary>
    /// Shared helper class for process and audio session management
    /// Consolidates duplicate logic between AudioSessionManager and ProcessDetectionService
    /// </summary>
    public static class ProcessSessionHelper
    {
        private static readonly CachedProcessProvider _processProvider = new();

        /// <summary>
        /// Normalizes process names by removing .exe extension and converting to lowercase
        /// </summary>
        /// <param name="processNames">Array of process names to normalize</param>
        /// <returns>Array of normalized process names</returns>
        public static string[] NormalizeProcessNames(string[] processNames)
        {
            return processNames
                .Select(name => name.ToLower().Replace(Constants.Audio.ExeExtension, ""))
                .ToArray();
        }

        /// <summary>
        /// Gets all audio sessions with their associated processes from the default audio device
        /// </summary>
        /// <param name="device">Audio device to enumerate sessions from</param>
        /// <returns>Enumerable of session-process pairs</returns>
        public static IEnumerable<(AudioSessionControl Session, Process Process)> GetAudioSessionsWithProcesses(MMDevice? device)
        {
            if (device?.AudioSessionManager?.Sessions == null)
                yield break;

            var sessionProcessPairs = new List<(AudioSessionControl, Process)>();

            try
            {
                for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                {
                    var session = device.AudioSessionManager.Sessions[i];
                    
                    // Skip system sessions
                    if (session.GetProcessID == Constants.Audio.SystemProcessId)
                        continue;

                    Process? process = null;
                    try
                    {
                        process = Process.GetProcessById((int)session.GetProcessID);
                        sessionProcessPairs.Add((session, process));
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists, skip
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error getting process for session PID {session.GetProcessID}", ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error enumerating audio sessions", ex);
            }

            foreach (var pair in sessionProcessPairs)
            {
                yield return pair;
            }
        }

        /// <summary>
        /// Gets audio sessions filtered by process names
        /// </summary>
        /// <param name="device">Audio device to enumerate sessions from</param>
        /// <param name="processNames">Array of process names to filter by</param>
        /// <returns>Enumerable of matching session-process pairs</returns>
        public static IEnumerable<(AudioSessionControl Session, Process Process)> GetProcessSessions(MMDevice? device, string[] processNames)
        {
            var normalizedNames = NormalizeProcessNames(processNames);
            
            return GetAudioSessionsWithProcesses(device)
                .Where(pair => normalizedNames.Contains(pair.Process.ProcessName.ToLower()));
        }

        /// <summary>
        /// Gets unique processes that have audio sessions
        /// </summary>
        /// <param name="device">Audio device to enumerate sessions from</param>
        /// <returns>Enumerable of unique processes with audio sessions</returns>
        public static IEnumerable<Process> GetUniqueProcessesWithAudio(MMDevice? device)
        {
            var seenProcessIds = new HashSet<int>();
            
            foreach (var (session, process) in GetAudioSessionsWithProcesses(device))
            {
                if (seenProcessIds.Add(process.Id))
                {
                    yield return process;
                }
            }
        }

        /// <summary>
        /// Checks if any of the specified processes are currently running using cached process provider
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <param name="forceRefresh">Force refresh of the process cache</param>
        /// <returns>True if any matching process is running, false otherwise</returns>
        public static bool IsAnyProcessRunning(string[] processNames, bool forceRefresh = false)
        {
            try
            {
                return _processProvider.AreAnyProcessesRunning(processNames, forceRefresh);
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking running processes", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the cached process provider instance
        /// </summary>
        public static CachedProcessProvider ProcessProvider => _processProvider;

        /// <summary>
        /// Disposes the cached process provider
        /// Should be called when the application shuts down
        /// </summary>
        public static void Dispose()
        {
            _processProvider?.Dispose();
        }
    }
} 