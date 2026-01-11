using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Lexus2_0.Desktop.Views
{
    public partial class IrctcIdManagerWindow : Window
    {
        private ObservableCollection<SavedIrctcId> _savedIds;
        private SavedIrctcId? _editingId;

        public IrctcIdManagerWindow()
        {
            InitializeComponent();
            _savedIds = new ObservableCollection<SavedIrctcId>();
            SavedIdsList.ItemsSource = _savedIds;
            LoadSavedIds();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SaveNameTextBox.Text))
                {
                    MessageBox.Show("Please enter a name to save this IRCTC ID.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(IrctcIdTextBox.Text))
                {
                    MessageBox.Show("Please enter IRCTC ID.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var password = PasswordBox.Password;
                if (string.IsNullOrWhiteSpace(password) && _editingId == null)
                {
                    MessageBox.Show("Please enter password.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_editingId != null)
                {
                    // Update existing ID
                    _editingId.SaveName = SaveNameTextBox.Text;
                    _editingId.IrctcId = IrctcIdTextBox.Text;
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        // Note: In real implementation, password should be encrypted
                        _editingId.Password = password;
                    }
                    _editingId.RememberMe = RememberMeCheckBox.IsChecked ?? false;
                    
                    // Refresh the list
                    var index = _savedIds.IndexOf(_editingId);
                    _savedIds[index] = _editingId;
                    _savedIds.RemoveAt(index);
                    _savedIds.Insert(index, _editingId);
                    _editingId = null;
                    
                    MessageBox.Show("IRCTC ID updated successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Add new ID
                    var newId = new SavedIrctcId
                    {
                        Id = Guid.NewGuid().ToString(),
                        SaveName = SaveNameTextBox.Text,
                        IrctcId = IrctcIdTextBox.Text,
                        Password = password, // Note: In real implementation, password should be encrypted
                        RememberMe = RememberMeCheckBox.IsChecked ?? false
                    };
                    
                    _savedIds.Add(newId);
                    MessageBox.Show("IRCTC ID saved successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                UpdateIdsList();
                // TODO: Save to database/file storage
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving IRCTC ID: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            SaveNameTextBox.Clear();
            IrctcIdTextBox.Clear();
            PasswordBox.Clear();
            RememberMeCheckBox.IsChecked = false;
            _editingId = null;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string id)
            {
                var savedId = _savedIds.FirstOrDefault(i => i.Id == id);
                if (savedId != null)
                {
                    _editingId = savedId;
                    SaveNameTextBox.Text = savedId.SaveName;
                    IrctcIdTextBox.Text = savedId.IrctcId;
                    RememberMeCheckBox.IsChecked = savedId.RememberMe;
                    
                    PasswordBox.Clear(); // Don't show password for security
                    
                    MessageBox.Show("Please update the IRCTC ID details and click Save. Password needs to be re-entered for security.", 
                        "Edit IRCTC ID", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string id)
            {
                var savedId = _savedIds.FirstOrDefault(i => i.Id == id);
                if (savedId != null)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete '{savedId.SaveName}'?", 
                        "Confirm Delete", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _savedIds.Remove(savedId);
                        UpdateIdsList();
                        // TODO: Delete from database/file storage
                        MessageBox.Show("IRCTC ID deleted successfully.", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadSavedIds()
        {
            // TODO: Load from database/file storage
            UpdateIdsList();
        }

        private void UpdateIdsList()
        {
            NoIdsTextBlock.Visibility = _savedIds.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }

    public class SavedIrctcId : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _saveName = string.Empty;
        private string _irctcId = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe = false;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string SaveName
        {
            get => _saveName;
            set { _saveName = value; OnPropertyChanged(); }
        }

        public string IrctcId
        {
            get => _irctcId;
            set { _irctcId = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set { _rememberMe = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

