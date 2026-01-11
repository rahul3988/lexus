using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages.Data
{
    public partial class AddPaymentOptionPage : UserControl
    {
        private PaymentOptionService? _paymentService;
        private ObservableCollection<PaymentOption> _paymentOptions = new();

        public PaymentOptionService? PaymentService
        {
            get => _paymentService;
            set
            {
                _paymentService = value;
                LoadPaymentOptions();
            }
        }

        public AddPaymentOptionPage()
        {
            InitializeComponent();
            PaymentOptionsGrid.ItemsSource = _paymentOptions;
        }

        private async void LoadPaymentOptions()
        {
            if (_paymentService == null) return;

            try
            {
                var options = await _paymentService.GetAllPaymentOptionsAsync();
                _paymentOptions.Clear();
                foreach (var option in options)
                {
                    _paymentOptions.Add(option);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_paymentService == null)
            {
                MessageBox.Show("Service not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var name = NameTextBox.Text;
            var type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var gateway = (GatewayComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var bankName = BankNameTextBox.Text;
            var cardNumber = CardNumberTextBox.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter Payment Option Name", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var option = new PaymentOption
                {
                    Name = name,
                    Type = type,
                    Gateway = gateway,
                    BankName = bankName,
                    CardNumber = cardNumber,
                    Status = "Active",
                    IsPriorBank = IsPriorBankCheckBox.IsChecked ?? false,
                    IsBackupBank = IsBackupBankCheckBox.IsChecked ?? false
                };

                await _paymentService.CreatePaymentOptionAsync(option);
                MessageBox.Show("Payment option added successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear form
                NameTextBox.Clear();
                BankNameTextBox.Clear();
                CardNumberTextBox.Clear();
                IsPriorBankCheckBox.IsChecked = false;
                IsBackupBankCheckBox.IsChecked = false;

                LoadPaymentOptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding payment option: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeletePayment_Click(object sender, RoutedEventArgs e)
        {
            if (_paymentService == null) return;

            if (sender is Button btn && btn.Tag is PaymentOption option)
            {
                var result = MessageBox.Show($"Are you sure you want to delete payment option '{option.Name}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _paymentService.DeletePaymentOptionAsync(option.Id);
                        LoadPaymentOptions();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting payment option: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}

