using System;
using System.Windows;
using System.Windows.Threading;
using Lexus2_0.Desktop.Utils;

namespace Lexus2_0.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize folders
            FolderManager.InitializeFolders();

            // Global exception handling
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true; // Prevent app crash
            HandleException(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception ex)
        {
            // Log error (could use FileLogger here)
            var errorMessage = $"Unhandled exception: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            
            // Show user-friendly error dialog
            MessageBox.Show(
                $"An error occurred: {ex.Message}\n\nThe application will continue running.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
