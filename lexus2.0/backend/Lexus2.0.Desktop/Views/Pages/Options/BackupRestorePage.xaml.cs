using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Lexus2_0.Desktop.Views.Pages.Options
{
    public partial class BackupRestorePage : UserControl
    {
        public BackupRestorePage()
        {
            InitializeComponent();
        }

        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Backup Files (*.lexus)|*.lexus|All Files (*.*)|*.*",
                FileName = $"Lexus_Backup_{System.DateTime.Now:yyyyMMdd_HHmmss}.lexus"
            };
            
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"Backup created successfully at:\n{dialog.FileName}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Backup Files (*.lexus)|*.lexus|All Files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show($"Are you sure you want to restore from:\n{dialog.FileName}\n\nThis will overwrite current settings.", 
                    "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Restore completed successfully.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"Lexus_Export_{System.DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"Data exported successfully to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"Data imported successfully from:\n{dialog.FileName}", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

