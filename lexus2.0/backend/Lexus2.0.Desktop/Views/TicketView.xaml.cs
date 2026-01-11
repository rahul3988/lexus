using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Lexus2_0.Desktop.ViewModels;

namespace Lexus2_0.Desktop.Views
{
    public partial class TicketView : UserControl
    {
        private TicketViewModel? _viewModel;
        private DispatcherTimer? _dateTimeTimer;

        public TicketViewModel ViewModel
        {
            get => _viewModel ?? throw new InvalidOperationException("ViewModel not initialized");
            set
            {
                _viewModel = value;
                DataContext = _viewModel;
                if (_viewModel != null)
                {
                    _ = _viewModel.LoadTicketsAsync(); // Fire and forget - intentional
                }
            }
        }

        public TicketView()
        {
            InitializeComponent();
            InitializeDateTimeDisplay();
        }

        private void InitializeDateTimeDisplay()
        {
            // Update date/time display every second
            _dateTimeTimer = new DispatcherTimer();
            _dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _dateTimeTimer.Tick += (s, e) => UpdateDateTimeDisplay();
            _dateTimeTimer.Start();
            UpdateDateTimeDisplay();
        }

        private void UpdateDateTimeDisplay()
        {
            var now = DateTime.Now;
            DateTimeTextBlock.Text = $"{now:yyyy-MM-dd}\n{now:HH:mm:ss}";
        }

        private async void CheckTokenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simple input dialog using TextBox
                var inputDialog = new Window
                {
                    Title = "Check Token",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this)
                };

                var stackPanel = new StackPanel { Margin = new Thickness(15) };
                stackPanel.Children.Add(new TextBlock { Text = "Enter token to validate:", Margin = new Thickness(0, 0, 0, 10) });
                
                var tokenTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(tokenTextBox);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var okButton = new Button { Content = "Validate", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "Cancel", Width = 80 };
                
                bool dialogResult = false;
                okButton.Click += (s, args) => { dialogResult = true; inputDialog.Close(); };
                cancelButton.Click += (s, args) => { inputDialog.Close(); };
                
                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);
                
                inputDialog.Content = stackPanel;
                tokenTextBox.Focus();
                inputDialog.ShowDialog();

                if (!dialogResult || string.IsNullOrWhiteSpace(tokenTextBox.Text))
                {
                    return;
                }

                var tokenInput = tokenTextBox.Text;

                // Validate token via API
                var apiClient = new Services.ApiClientService();
                var isValid = await apiClient.ValidateTokenAsync(tokenInput);

                if (isValid)
                {
                    UserTokenTextBlock.Text = tokenInput;
                    ValidCountTextBlock.Text = "1";
                    MessageBox.Show("Token is valid!", "Token Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Token is invalid or validation failed.", "Token Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking token: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            // Handle Web2/Web3 checkbox changes
        }

        private void SmsUploaderLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("SMS Uploader functionality - To be implemented", 
                "SMS Uploader", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open SMS Uploader window
        }

        private void TicketRow_DoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row && row.Item != null)
            {
                OpenButton_Click(sender, e);
            }
        }

        private async void NewTicketButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewTicketDialog();
            if (dialog.ShowDialog() == true && dialog.CreatedTicket != null && _viewModel != null)
            {
                // Ticket creation will be handled by the service that creates it
                await _viewModel.LoadTicketsAsync();
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ticketId)
            {
                MessageBox.Show($"Open ticket {ticketId} functionality - To be implemented.\n\nThis will open the ticket form for viewing/editing.", 
                    "Open Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Open ticket form/dialog with ticket details
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ticketId && _viewModel != null)
            {
                try
                {
                    MessageBox.Show($"Login process for ticket {ticketId} - To be implemented.\n\nThis will start the login/booking process for this ticket.", 
                        "Login Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
                    // TODO: Start login/automation process for this ticket
                    // await _viewModel.StartTicketAsync(ticketId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ticketId)
            {
                MessageBox.Show($"Edit ticket {ticketId} functionality - To be implemented.\n\nThis will open the ticket form for editing.", 
                    "Edit Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Open ticket form/dialog in edit mode
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ticketId && _viewModel != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete ticket {ticketId}?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _viewModel.DeleteTicketAsync(ticketId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
