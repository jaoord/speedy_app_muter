using SpeedyAppMuter.UI;
using SpeedyAppMuter.Utils;
using SpeedyAppMuter.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SpeedyAppMuter
{
    internal class Program
    {
        private static SystemTrayApp? _app;
        private static Mutex? _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            // Ensure only one instance of the application is running
            _mutex = new Mutex(true, Constants.Application.MutexName, out bool createdNew);

            if (!createdNew)
            {
                Logger.Warning($"Another instance of {Constants.Application.ApplicationName} is already running.");
                MessageBox.Show(Constants.Messages.AlreadyRunningMessage, 
                    Constants.Messages.AlreadyRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Set up Windows Forms application
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);

                // Handle unhandled exceptions
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                Logger.Info($"Starting {Constants.Application.ApplicationName}...");

                // Create and run the system tray application
                _app = new SystemTrayApp();
                
                // Set up graceful shutdown
                Application.ApplicationExit += OnApplicationExit;
                Console.CancelKeyPress += OnCancelKeyPress;

                Logger.Info("Application initialized successfully. Running...");
                
                // Run the application
                _app.Run();
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error occurred", ex);
                MessageBox.Show($"A fatal error occurred: {ex.Message}", 
                    Constants.Messages.FatalErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Clean up
                _app?.Dispose();
                
                // Dispose the shared process provider
                ProcessSessionHelper.Dispose();
                
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                Logger.Info("Application shutdown complete.");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.Error("Thread exception occurred", e.Exception);
            
            var result = MessageBox.Show(
                $"An error occurred: {e.Exception.Message}\n\nDo you want to continue running the application?",
                Constants.Messages.ErrorTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                
            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error($"Unhandled exception: {e.ExceptionObject}");
            
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred: {ex.Message}", 
                    Constants.Messages.FatalErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            // The application will terminate after this handler
        }

        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            Logger.Info("Application exit event triggered");
            _app?.Dispose();
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            Logger.Info("Cancel key press detected");
            e.Cancel = true; // Prevent immediate termination
            Application.Exit();
        }
    }
} 