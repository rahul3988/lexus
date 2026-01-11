using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages.Data
{
    public partial class AddIrctcIdPage : UserControl
    {
        private IrctcAccountService? _accountService;
        private ObservableCollection<IrctcAccount> _accounts = new();

        public IrctcAccountService? AccountService
        {
            get => _accountService;
            set
            {
                _accountService = value;
                LoadAccounts();
            }
        }

        public AddIrctcIdPage()
        {
            InitializeComponent();
            IrctcIdsGrid.ItemsSource = _accounts;
        }

        private async void LoadAccounts()
        {
            if (_accountService == null) return;

            try
            {
                var accounts = await _accountService.GetAllAccountsAsync();
                _accounts.Clear();
                foreach (var account in accounts)
                {
                    _accounts.Add(account);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading accounts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_accountService == null)
            {
                MessageBox.Show("Service not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var id = IrctcIdTextBox.Text;
            var password = PasswordBox.Password;
            var mobile = MobileTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Please enter IRCTC ID", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter Password", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(mobile))
            {
                MessageBox.Show("Please enter Mobile Number", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var account = new IrctcAccount
                {
                    IrctcId = id,
                    Password = password,
                    MobileNumber = mobile,
                    Status = "Active"
                };

                await _accountService.CreateAccountAsync(account);
                MessageBox.Show("IRCTC ID added successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Clear form
                IrctcIdTextBox.Clear();
                PasswordBox.Clear();
                MobileTextBox.Clear();
                
                // Reload accounts
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding account: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

