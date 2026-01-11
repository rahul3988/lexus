using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Utils;

namespace Lexus2_0.Desktop.Views.Pages.Options
{
    public partial class ResetLexusIdPage : UserControl
    {
        public ResetLexusIdPage()
        {
            InitializeComponent();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmation = ConfirmTextBox.Text;

            if (confirmation != "RESET")
            {
                MessageBox.Show("Please type 'RESET' to confirm", "Confirmation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you absolutely sure you want to reset your Lexus ID?\n\nThis will clear all saved credentials and require re-authentication.",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Clear cache and credentials
                    if (Directory.Exists(FolderManager.CachePath))
                    {
                        var files = Directory.GetFiles(FolderManager.CachePath, "*credentials*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }

                    // Clear cookies
                    if (Directory.Exists(FolderManager.CachePath))
                    {
                        var cookieFiles = Directory.GetFiles(FolderManager.CachePath, "*cookie*", SearchOption.AllDirectories);
                        foreach (var file in cookieFiles)
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }

                    MessageBox.Show(
                        "Lexus ID has been reset successfully.\n\nPlease restart the application and log in again.",
                        "Reset Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    ConfirmTextBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting Lexus ID: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

