# Speedy App Muter

A Windows taskbar application that allows you to mute specific applications (like Firefox) using configurable hotkeys. Perfect for quickly muting ads during live TV streaming.

## Features

- **System Tray Integration**: Runs quietly in the background with a system tray icon
- **Global Hotkeys**: Mute/unmute applications even when they're not in focus
- **JSON Configuration**: Easy to configure target applications and hotkeys
- **Visual Feedback**: Tray icon changes color to indicate mute state
- **Context Menu**: Right-click for quick actions and status information
- **Single Instance**: Prevents multiple instances from running simultaneously

## Default Configuration

By default, the application is configured to mute Firefox using `Ctrl+Alt+F9`. The configuration is stored in `config.json`.

## Building the Application

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11

### Build Steps
1. Clone or download the source code
2. Open a command prompt in the project directory
3. Run: `dotnet build --configuration Release`
4. The executable will be in `bin/Release/net8.0-windows/`

### Running the Application
1. Run `SpeedyAppMuter.exe`
2. The application will start minimized to the system tray
3. Look for the blue "M" icon in your system tray

## Configuration

The application uses a `config.json` file for configuration. Here's the default configuration:

```json
{
  "targetApplication": {
    "name": "Firefox",
    "processNames": ["firefox", "firefox.exe"],
    "hotkey": {
      "key": "F9",
      "modifiers": ["Ctrl", "Alt"]
    }
  },
  "settings": {
    "startMinimized": true,
    "showTrayIcon": true
  }
}
```

### Configuration Options

- **name**: Display name for the target application
- **processNames**: Array of process names to target (with or without .exe)
- **hotkey.key**: The key to press (F1-F24, A-Z, Space, Enter, Escape)
- **hotkey.modifiers**: Array of modifier keys (Ctrl, Alt, Shift, Win)
- **startMinimized**: Whether to start minimized to system tray
- **showTrayIcon**: Whether to show the system tray icon

### Changing the Target Application

To target a different application (e.g., Chrome):

1. Right-click the system tray icon and select "Open Config File"
2. Edit the configuration:
   ```json
   {
     "targetApplication": {
       "name": "Chrome",
       "processNames": ["chrome", "chrome.exe"],
       "hotkey": {
         "key": "M",
         "modifiers": ["Ctrl", "Shift"]
       }
     }
   }
   ```
3. Save the file
4. Right-click the system tray icon and select "Reload Config"

## Usage

1. **Start the application**: Run `SpeedyAppMuter.exe`
2. **Mute/Unmute**: Press the configured hotkey (default: `Ctrl+Alt+F9`)
3. **System Tray**: 
   - Blue icon = Application running/unmuted
   - Red icon = Application muted
   - Double-click to toggle mute
   - Right-click for context menu

### Context Menu Options
- **Toggle Mute**: Manually toggle the mute state
- **Status**: Shows current state (Running/Muted/Not Running)
- **Target**: Shows the configured target application
- **Hotkey**: Shows the configured hotkey combination
- **Open Config File**: Opens the configuration file in the default editor
- **Reload Config**: Reloads the configuration without restarting
- **Exit**: Closes the application

## Troubleshooting

### Hotkey Not Working
- Check if another application is using the same hotkey combination
- Try a different key combination in the configuration
- Make sure the application is running (check system tray)

### Target Application Not Found
- Verify the process names in the configuration match the actual process names
- Use Task Manager to check the exact process name
- Make sure the target application is actually running

### Audio Not Muting
- Ensure the target application is actually playing audio
- Check Windows Volume Mixer to see if the application appears there
- Try running the application as Administrator if needed

## Technical Details

The application uses:
- **NAudio**: For Windows Core Audio API integration
- **Win32 APIs**: For global hotkey registration
- **Windows Forms**: For system tray integration
- **System.Text.Json**: For configuration management

## License

This project is provided as-is for educational and personal use. 