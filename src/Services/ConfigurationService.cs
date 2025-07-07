using SpeedyAppMuter.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SpeedyAppMuter.Services
{
    public class ConfigurationService
    {
        private readonly string _configFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationService(string configFilePath = "config.json")
        {
            _configFilePath = configFilePath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Loads the configuration from the JSON file
        /// </summary>
        /// <returns>The loaded configuration, or default configuration if file doesn't exist or is invalid</returns>
        public AppConfig LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    Debug.WriteLine($"Configuration file not found: {_configFilePath}. Creating default configuration.");
                    var defaultConfig = CreateDefaultConfiguration();
                    SaveConfiguration(defaultConfig);
                    return defaultConfig;
                }

                var jsonContent = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonContent, _jsonOptions);
                
                if (config == null)
                {
                    Debug.WriteLine("Failed to deserialize configuration. Using default configuration.");
                    return CreateDefaultConfiguration();
                }

                Debug.WriteLine($"Configuration loaded successfully from {_configFilePath}");
                return config;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading configuration: {ex.Message}. Using default configuration.");
                return CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Saves the configuration to the JSON file
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SaveConfiguration(AppConfig config)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_configFilePath, jsonContent);
                Debug.WriteLine($"Configuration saved successfully to {_configFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        /// <returns>Default AppConfig instance</returns>
        private static AppConfig CreateDefaultConfiguration()
        {
            return new AppConfig
            {
                TargetApplication = new TargetApplication
                {
                    Name = "Firefox",
                    ProcessNames = ["firefox", "firefox.exe"],
                    Hotkey = new HotkeyConfig
                    {
                        Key = "F9",
                        Modifiers = ["Ctrl", "Alt"]
                    }
                },
                Settings = new AppSettings()
            };
        }

        /// <summary>
        /// Validates the configuration and fixes common issues
        /// </summary>
        /// <param name="config">The configuration to validate</param>
        /// <returns>True if the configuration is valid or was successfully fixed, false otherwise</returns>
        public bool ValidateConfiguration(AppConfig config)
        {
            bool isValid = true;

            // Validate target application
            if (config.TargetApplication == null)
            {
                Debug.WriteLine("Target application is null. Using default.");
                config.TargetApplication = CreateDefaultConfiguration().TargetApplication;
                isValid = false;
            }

            // Validate process names
            if (config.TargetApplication.ProcessNames == null || config.TargetApplication.ProcessNames.Length == 0)
            {
                Debug.WriteLine("Process names are empty. Using default.");
                config.TargetApplication.ProcessNames = ["firefox", "firefox.exe"];
                isValid = false;
            }

            // Validate hotkey
            if (config.TargetApplication.Hotkey == null)
            {
                Debug.WriteLine("Hotkey configuration is null. Using default.");
                config.TargetApplication.Hotkey = CreateDefaultConfiguration().TargetApplication.Hotkey;
                isValid = false;
            }

            // Validate hotkey key
            if (string.IsNullOrEmpty(config.TargetApplication.Hotkey.Key))
            {
                Debug.WriteLine("Hotkey key is empty. Using default.");
                config.TargetApplication.Hotkey.Key = "F9";
                isValid = false;
            }

            // Validate hotkey modifiers
            if (config.TargetApplication.Hotkey.Modifiers == null || config.TargetApplication.Hotkey.Modifiers.Length == 0)
            {
                Debug.WriteLine("Hotkey modifiers are empty. Using default.");
                config.TargetApplication.Hotkey.Modifiers = ["Ctrl", "Alt"];
                isValid = false;
            }

            // Validate settings
            if (config.Settings == null)
            {
                Debug.WriteLine("Settings are null. Using default.");
                config.Settings = CreateDefaultConfiguration().Settings;
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        /// <returns>The absolute path to the configuration file</returns>
        public string GetConfigFilePath()
        {
            return Path.GetFullPath(_configFilePath);
        }
    }
} 