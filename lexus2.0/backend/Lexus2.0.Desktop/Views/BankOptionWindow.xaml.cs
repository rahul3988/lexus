using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Lexus2_0.Desktop.Views
{
    public partial class BankOptionWindow : Window
    {
        private ObservableCollection<SavedPaymentMethod> _savedPayments;
        private string _currentTab = "Bank";
        private SavedPaymentMethod? _editingPayment;

        public BankOptionWindow()
        {
            InitializeComponent();
            _savedPayments = new ObservableCollection<SavedPaymentMethod>();
            SavedPaymentsList.ItemsSource = _savedPayments;
            UpdateTabButtonStyles();
            LoadSavedPayments();
        }

        private void UpdateTabButtonStyles()
        {
            // Reset all buttons to secondary style
            BankTabButton.Style = (Style)FindResource("SecondaryButton");
            DebitCardTabButton.Style = (Style)FindResource("SecondaryButton");
            CreditCardTabButton.Style = (Style)FindResource("SecondaryButton");

            // Set active tab to primary style
            switch (_currentTab)
            {
                case "Bank":
                    BankTabButton.Style = (Style)FindResource("PrimaryButton");
                    PaymentTypeLabel.Text = "Bank Name:";
                    break;
                case "DebitCard":
                    DebitCardTabButton.Style = (Style)FindResource("PrimaryButton");
                    PaymentTypeLabel.Text = "Card Bank Name:";
                    break;
                case "CreditCard":
                    CreditCardTabButton.Style = (Style)FindResource("PrimaryButton");
                    PaymentTypeLabel.Text = "Card Bank Name:";
                    break;
            }
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                _currentTab = tabName;
                UpdateTabButtonStyles();
                ClearForm();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SaveNameTextBox.Text))
                {
                    MessageBox.Show("Please enter a name to save this payment method.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    MessageBox.Show("Please enter username/account ID.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (BankNameComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a bank name.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var bankName = (BankNameComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                var password = PasswordBox.Password;

                if (_editingPayment != null)
                {
                    // Update existing payment
                    _editingPayment.SaveName = SaveNameTextBox.Text;
                    _editingPayment.Username = UsernameTextBox.Text;
                    _editingPayment.BankName = bankName;
                    _editingPayment.PaymentType = _currentTab;
                    // Note: In real implementation, password should be encrypted
                    
                    // Refresh the list
                    var index = _savedPayments.IndexOf(_editingPayment);
                    _savedPayments[index] = _editingPayment;
                    _savedPayments.RemoveAt(index);
                    _savedPayments.Insert(index, _editingPayment);
                    _editingPayment = null;
                    
                    MessageBox.Show("Payment method updated successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Add new payment
                    var newPayment = new SavedPaymentMethod
                    {
                        Id = Guid.NewGuid().ToString(),
                        SaveName = SaveNameTextBox.Text,
                        Username = UsernameTextBox.Text,
                        BankName = bankName,
                        PaymentType = _currentTab
                        // Note: In real implementation, password should be encrypted and stored securely
                    };
                    
                    _savedPayments.Add(newPayment);
                    MessageBox.Show("Payment method saved successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                UpdatePaymentsList();
                // TODO: Save to database/file storage
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment method: {ex.Message}", 
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
            UsernameTextBox.Clear();
            PasswordBox.Clear();
            BankNameComboBox.SelectedIndex = -1;
            _editingPayment = null;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string paymentId)
            {
                var payment = _savedPayments.FirstOrDefault(p => p.Id == paymentId);
                if (payment != null)
                {
                    _editingPayment = payment;
                    SaveNameTextBox.Text = payment.SaveName;
                    UsernameTextBox.Text = payment.Username;
                    
                    // Select the bank name in combobox
                    foreach (ComboBoxItem item in BankNameComboBox.Items)
                    {
                        if (item.Content?.ToString() == payment.BankName)
                        {
                            BankNameComboBox.SelectedItem = item;
                            break;
                        }
                    }
                    
                    // Switch to the correct tab
                    _currentTab = payment.PaymentType;
                    UpdateTabButtonStyles();
                    
                    PasswordBox.Clear(); // Don't show password for security
                    
                    MessageBox.Show("Please update the payment details and click Save. Password needs to be re-entered for security.", 
                        "Edit Payment", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string paymentId)
            {
                var payment = _savedPayments.FirstOrDefault(p => p.Id == paymentId);
                if (payment != null)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete '{payment.SaveName}'?", 
                        "Confirm Delete", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _savedPayments.Remove(payment);
                        UpdatePaymentsList();
                        // TODO: Delete from database/file storage
                        MessageBox.Show("Payment method deleted successfully.", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadSavedPayments()
        {
            // TODO: Load from database/file storage
            UpdatePaymentsList();
        }

        private void UpdatePaymentsList()
        {
            NoPaymentsTextBlock.Visibility = _savedPayments.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }

    public class SavedPaymentMethod : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _saveName = string.Empty;
        private string _username = string.Empty;
        private string _bankName = string.Empty;
        private string _paymentType = "Bank";

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string SaveName
        {
            get => _saveName;
            set { _saveName = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaymentTypeDisplay)); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string BankName
        {
            get => _bankName;
            set { _bankName = value; OnPropertyChanged(); OnPropertyChanged(nameof(BankNameDisplay)); }
        }

        public string PaymentType
        {
            get => _paymentType;
            set { _paymentType = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaymentTypeDisplay)); }
        }

        public string PaymentTypeDisplay
        {
            get
            {
                return PaymentType switch
                {
                    "Bank" => "Bank Account",
                    "DebitCard" => "Debit Card",
                    "CreditCard" => "Credit Card",
                    _ => "Unknown"
                };
            }
        }

        public string BankNameDisplay => $"{PaymentTypeDisplay} - {BankName}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

