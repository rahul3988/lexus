using System;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages.Bypass
{
    public partial class CheckTokenPage : UserControl
    {
        private ApiClientService? _apiClient;
        private int _validCount = 0;

        public ApiClientService? ApiClient
        {
            get => _apiClient;
            set => _apiClient = value ?? new ApiClientService();
        }

        public CheckTokenPage()
        {
            InitializeComponent();
            _apiClient = new ApiClientService();
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var token = TokenTextBox.Text;

            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Please enter a token", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ValidateButton.IsEnabled = false;
                ValidateButton.Content = "Validating...";

                var isValid = await _apiClient!.ValidateTokenAsync(token);

                if (isValid)
                {
                    TokenStatusTextBlock.Text = "Valid";
                    TokenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    _validCount++;
                    ValidCountTextBlock.Text = _validCount.ToString();
                    MessageBox.Show("Token is valid!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    TokenStatusTextBlock.Text = "Invalid";
                    TokenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show("Token is invalid or validation failed.", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating token: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TokenStatusTextBlock.Text = "Error";
                TokenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                ValidateButton.IsEnabled = true;
                ValidateButton.Content = "Validate Token";
            }
        }
    }
}

