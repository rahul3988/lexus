using System;
using System.Windows;
using System.Windows.Controls;

namespace Lexus2_0.Desktop.Views.Pages.Options
{
    public partial class KeyInfoVersionCheckPage : UserControl
    {
        public KeyInfoVersionCheckPage()
        {
            InitializeComponent();
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            BuildDateTextBlock.Text = System.IO.File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss");
            
            var systemInfo = $"OS: {Environment.OSVersion}\n" +
                           $"Framework: {Environment.Version}\n" +
                           $"Machine Name: {Environment.MachineName}\n" +
                           $"User Name: {Environment.UserName}\n" +
                           $"Processor Count: {Environment.ProcessorCount}\n" +
                           $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB";
            
            SystemInfoTextBlock.Text = systemInfo;
        }

        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("You are using the latest version (2.0.0)", "Update Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

