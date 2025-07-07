using SpeedyAppMuter.UI;
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
            const string mutexName = "SpeedyAppMuter_SingleInstance";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                Debug.WriteLine("Another instance of Speedy App Muter is already running.");
                MessageBox.Show("Speedy App Muter is already running. Check your system tray.", 
                    "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                Debug.WriteLine("Starting Speedy App Muter...");

                // Create and run the system tray application
                _app = new SystemTrayApp();
                
                // Set up graceful shutdown
                Application.ApplicationExit += OnApplicationExit;
                Console.CancelKeyPress += OnCancelKeyPress;

                Debug.WriteLine("Application initialized successfully. Running...");
                
                // Run the application
                _app.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error: {ex.Message}");
                MessageBox.Show($"A fatal error occurred: {ex.Message}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Clean up
                _app?.Dispose();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                Debug.WriteLine("Application shutdown complete.");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Debug.WriteLine($"Thread exception: {e.Exception.Message}");
            
            var result = MessageBox.Show(
                $"An error occurred: {e.Exception.Message}\n\nDo you want to continue running the application?",
                "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                
            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred: {ex.Message}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            // The application will terminate after this handler
        }

        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            Debug.WriteLine("Application exit event triggered.");
            _app?.Dispose();
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            Debug.WriteLine("Cancel key press detected. Shutting down gracefully...");
            e.Cancel = true; // Prevent immediate termination
            Application.Exit();
        }
    }
} 