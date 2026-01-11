using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Services;
using Lexus2_0.Desktop.Utils;

namespace Lexus2_0.Desktop.Views
{
    public partial class ControlView : UserControl
    {
        private AutomationService? _automationService;
        private System.Windows.Threading.DispatcherTimer? _statusTimer;

        public ControlView()
        {
            InitializeComponent();
        }

        public void SetAutomationService(AutomationService automationService)
        {
            _automationService = automationService;
            if (_automationService != null)
            {
                _automationService.LogMessage += OnLogMessage;
                UpdateStatus();
                
                // Subscribe to status changes
                _statusTimer = new System.Windows.Threading.DispatcherTimer();
                _statusTimer.Interval = TimeSpan.FromSeconds(1);
                _statusTimer.Tick += (s, e) => UpdateStatus();
                _statusTimer.Start();
            }
        }

        private void UpdateStatus()
        {
            if (_automationService == null) return;

            Dispatcher.Invoke(() =>
            {
                var isRunning = _automationService.IsRunning;
                StatusTextBlock.Text = isRunning ? "Running" : "Idle";
                StatusTextBlock.Foreground = isRunning 
                    ? System.Windows.Media.Brushes.LightBlue 
                    : System.Windows.Media.Brushes.LightGreen;
                
                StopButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
                StopButton.IsEnabled = isRunning;
            });
        }

        private void OnLogMessage(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogsTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                LogsTextBox.ScrollToEnd();
                
                // Limit log display to last 1000 lines
                var lines = LogsTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length > 1000)
                {
                    var recentLines = lines.Skip(lines.Length - 1000);
                    LogsTextBox.Text = string.Join(Environment.NewLine, recentLines);
                }
            });
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_automationService != null)
            {
                try
                {
                    StopButton.IsEnabled = false;
                    await _automationService.StopCurrentTicketAsync();
                    UpdateStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error stopping automation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    StopButton.IsEnabled = true;
                }
            }
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            // Settings will be saved when implemented
        }

        private void AlwaysOnTop_Changed(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Topmost = AlwaysOnTopCheckBox.IsChecked ?? false;
            }
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(FolderManager.CachePath))
                {
                    var files = Directory.GetFiles(FolderManager.CachePath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                    }
                }
                MessageBox.Show("Cache cleared successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing cache: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(FolderManager.LogsPath))
                {
                    Process.Start("explorer.exe", FolderManager.LogsPath);
                }
                else
                {
                    MessageBox.Show("Logs folder does not exist", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Backup Restore functionality - To be implemented", 
                "Backup Restore", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Backup/Restore dialog
        }

        private void StepsOfAdvPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Steps Of Adv Payment functionality - To be implemented", 
                "Advanced Payment", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Advanced Payment Configuration
        }

        private void BhimUpiDirectPayButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("BHIMUPI Direct Pay functionality - To be implemented", 
                "BHIM UPI Direct Pay", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Toggle BHIM UPI Direct Pay
        }

        private void KeyInfoVersionCheckButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Key Info / Version Check functionality - To be implemented", 
                "License Info", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Show License/Version Information Dialog
        }

        private void ShowHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Show History functionality - To be implemented", 
                "Show History", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open History/Logs Window
        }

        private void SystemRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("System Refresh functionality - To be implemented", 
                "System Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Refresh system/application state
        }

        private void OptimizeSystemButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Optimize System functionality - To be implemented", 
                "Optimize System", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open System Optimization tool
        }

        private void CreateIrctcIdButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Create IRCTC ID functionality - To be implemented", 
                "Create IRCTC ID", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Create IRCTC ID dialog
        }

        private void ResetLexusIdButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset your Lexus ID? This action cannot be undone.", 
                "Reset Lexus ID", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Reset Lexus ID functionality - To be implemented", 
                    "Reset Lexus ID", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Reset Lexus ID/login credentials
            }
        }

        public void HandleMenuItemAction(string action)
        {
            switch (action)
            {
                case "BackupRestore":
                    BackupRestoreButton_Click(null, null);
                    break;
                case "StepsOfAdvPayment":
                    StepsOfAdvPaymentButton_Click(null, null);
                    break;
                case "KeyInfoVersionCheck":
                    KeyInfoVersionCheckButton_Click(null, null);
                    break;
                case "ShowHistory":
                    ShowHistoryButton_Click(null, null);
                    break;
                case "SystemRefresh":
                    SystemRefreshButton_Click(null, null);
                    break;
                case "OptimizeSystem":
                    OptimizeSystemButton_Click(null, null);
                    break;
                case "CreateIrctcId":
                    CreateIrctcIdButton_Click(null, null);
                    break;
                case "BhimUpiDirectPay":
                    BhimUpiDirectPayButton_Click(null, null);
                    break;
                case "DishaLogin":
                    DishaLoginButton_Click(null, null);
                    break;
                case "FailIssueFix":
                    FailIssueFixButton_Click(null, null);
                    break;
                case "ResetLexusId":
                    ResetLexusIdButton_Click(null, null);
                    break;
            }
        }

        private void DishaLoginButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Disha Login on/off functionality - To be implemented", 
                "Disha Login", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Toggle Disha Login method
        }

        private void FailIssueFixButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fail Issue Fix functionality - To be implemented", 
                "Fail Issue Fix", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Troubleshooting/Fix tool
        }
    }
}
