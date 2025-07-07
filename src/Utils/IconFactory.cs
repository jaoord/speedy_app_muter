using SpeedyAppMuter.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeedyAppMuter.Utils
{
    /// <summary>
    /// Factory class for creating system tray icons
    /// Consolidates duplicate icon creation logic from SystemTrayApp
    /// </summary>
    public static class IconFactory
    {
        /// <summary>
        /// Creates a system tray icon with the specified mute state
        /// </summary>
        /// <param name="isMuted">True for muted (red) icon, false for unmuted (blue) icon</param>
        /// <returns>Icon instance for the system tray</returns>
        public static Icon CreateMuteIcon(bool isMuted)
        {
            var bitmap = new Bitmap(Constants.UI.TrayIconSize, Constants.UI.TrayIconSize);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // Clear background
                graphics.Clear(Constants.Colors.TransparentColor);
                
                // Choose color based on mute state
                var circleColor = isMuted ? Constants.Colors.MutedColor : Constants.Colors.UnmutedColor;
                using (var brush = new SolidBrush(circleColor))
                {
                    graphics.FillEllipse(brush, 
                        Constants.UI.IconCircleX, 
                        Constants.UI.IconCircleY, 
                        Constants.UI.IconCircleSize, 
                        Constants.UI.IconCircleSize);
                }
                
                // Draw the "M" text
                using (var font = new Font(Constants.UI.IconFontFamily, Constants.UI.IconFontSize, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Constants.Colors.IconTextColor))
                {
                    graphics.DrawString(Constants.UI.IconText, font, textBrush, 
                        Constants.UI.IconTextX, Constants.UI.IconTextY);
                }
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// Creates the default application icon (unmuted state)
        /// </summary>
        /// <returns>Default application icon</returns>
        public static Icon CreateDefaultIcon()
        {
            return CreateMuteIcon(false);
        }

        /// <summary>
        /// Safely disposes an icon if it's not null
        /// </summary>
        /// <param name="icon">Icon to dispose</param>
        public static void SafeDisposeIcon(Icon? icon)
        {
            try
            {
                icon?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing icon: {ex.Message}");
            }
        }
    }
} 