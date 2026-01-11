using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Utils;

namespace Lexus2_0.Desktop.Views.Pages.Options
{
    public partial class SystemRefreshPage : UserControl
    {
        public SystemRefreshPage()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Refreshing...";
                RefreshButton.IsEnabled = false;

                if (ClearCacheCheckBox.IsChecked == true)
                {
                    ClearCache();
                }

                if (ClearLogsCheckBox.IsChecked == true)
                {
                    ClearLogs();
                }

                if (RefreshServicesCheckBox.IsChecked == true)
                {
                    RefreshServices();
                }

                if (ReloadDataCheckBox.IsChecked == true)
                {
                    ReloadData();
                }

                StatusTextBlock.Text = "System refreshed successfully!";
                MessageBox.Show("System refresh completed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Error refreshing system: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void ClearCache()
        {
            if (Directory.Exists(FolderManager.CachePath))
            {
                var files = Directory.GetFiles(FolderManager.CachePath);
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private void ClearLogs()
        {
            if (Directory.Exists(FolderManager.LogsPath))
            {
                var files = Directory.GetFiles(FolderManager.LogsPath);
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private void RefreshServices()
        {
            // Trigger garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void ReloadData()
        {
            // Data will be reloaded when pages are accessed
        }
    }
}

