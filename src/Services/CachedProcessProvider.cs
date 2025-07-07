using SpeedyAppMuter.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpeedyAppMuter.Services
{
    /// <summary>
    /// Provides cached access to running processes to improve performance
    /// Reduces frequent Process.GetProcesses() calls by caching results
    /// </summary>
    public class CachedProcessProvider : IDisposable
    {
        private readonly TimeSpan _cacheExpiration;
        private readonly ILogger _logger;
        private DateTime _lastRefresh;
        private List<Process> _cachedProcesses = new();
        private readonly object _lock = new();
        private bool _disposed = false;

        public CachedProcessProvider(ILogger? logger = null, TimeSpan? cacheExpiration = null)
        {
            _logger = logger ?? Logger.Instance;
            _cacheExpiration = cacheExpiration ?? TimeSpan.FromSeconds(Constants.Performance.ProcessCacheExpirationSeconds);
            _lastRefresh = DateTime.MinValue;
        }

        /// <summary>
        /// Gets all running processes, using cached results if available and not expired
        /// </summary>
        /// <param name="forceRefresh">Force refresh of the cache regardless of expiration</param>
        /// <returns>List of running processes</returns>
        public List<Process> GetProcesses(bool forceRefresh = false)
        {
            lock (_lock)
            {
                if (_disposed)
                    return new List<Process>();

                if (forceRefresh || ShouldRefreshCache())
                {
                    RefreshCache();
                }

                // Return a copy to prevent external modification
                return new List<Process>(_cachedProcesses);
            }
        }

        /// <summary>
        /// Gets processes filtered by name, using cached results
        /// </summary>
        /// <param name="processNames">Array of process names to filter by</param>
        /// <param name="forceRefresh">Force refresh of the cache</param>
        /// <returns>List of matching processes</returns>
        public List<Process> GetProcessesByName(string[] processNames, bool forceRefresh = false)
        {
            var normalizedNames = ProcessSessionHelper.NormalizeProcessNames(processNames);
            var allProcesses = GetProcesses(forceRefresh);
            
            return allProcesses
                .Where(p => normalizedNames.Contains(p.ProcessName.ToLower()))
                .ToList();
        }

        /// <summary>
        /// Checks if any of the specified processes are running, using cached results
        /// </summary>
        /// <param name="processNames">Array of process names to check</param>
        /// <param name="forceRefresh">Force refresh of the cache</param>
        /// <returns>True if any matching process is running</returns>
        public bool AreAnyProcessesRunning(string[] processNames, bool forceRefresh = false)
        {
            var normalizedNames = ProcessSessionHelper.NormalizeProcessNames(processNames);
            var allProcesses = GetProcesses(forceRefresh);
            
            return allProcesses.Any(p => normalizedNames.Contains(p.ProcessName.ToLower()));
        }

        /// <summary>
        /// Gets the count of cached processes
        /// </summary>
        public int CachedProcessCount
        {
            get
            {
                lock (_lock)
                {
                    return _cachedProcesses.Count;
                }
            }
        }

        /// <summary>
        /// Gets the time when the cache was last refreshed
        /// </summary>
        public DateTime LastRefreshTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastRefresh;
                }
            }
        }

        /// <summary>
        /// Gets whether the cache is expired
        /// </summary>
        public bool IsCacheExpired
        {
            get
            {
                lock (_lock)
                {
                    return ShouldRefreshCache();
                }
            }
        }

        /// <summary>
        /// Clears the cache and forces a refresh on the next access
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _cachedProcesses.Clear();
                _lastRefresh = DateTime.MinValue;
                _logger.LogDebug("Process cache cleared");
            }
        }

        private bool ShouldRefreshCache()
        {
            return DateTime.Now - _lastRefresh > _cacheExpiration;
        }

        private void RefreshCache()
        {
            try
            {
                _logger.LogDebug("Refreshing process cache");
                
                // Clear old processes
                _cachedProcesses.Clear();
                
                // Get fresh process list
                var processes = Process.GetProcesses();
                _cachedProcesses.AddRange(processes);
                
                _lastRefresh = DateTime.Now;
                
                _logger.LogDebug($"Process cache refreshed with {_cachedProcesses.Count} processes");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing process cache", ex);
                
                // If refresh fails, keep the old cache but mark it as expired
                _lastRefresh = DateTime.MinValue;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    _cachedProcesses.Clear();
                    _disposed = true;
                }
                
                _logger.LogDebug("CachedProcessProvider disposed");
            }

            GC.SuppressFinalize(this);
        }
    }
} 