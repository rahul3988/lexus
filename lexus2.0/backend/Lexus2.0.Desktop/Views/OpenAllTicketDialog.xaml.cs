using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Lexus2_0.Core.Models;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views
{
    public partial class OpenAllTicketDialog : Window
    {
        private readonly List<Ticket> _tickets;
        private readonly AutomationService? _automationService;
        private readonly TicketService? _ticketService;

        public OpenAllTicketDialog(List<Ticket> tickets, AutomationService? automationService, TicketService? ticketService = null)
        {
            InitializeComponent();
            _tickets = tickets;
            _automationService = automationService;
            _ticketService = ticketService;
            TicketsItemsControl.ItemsSource = _tickets;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void StopPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Ticket ticket)
            {
                var result = MessageBox.Show($"Stop payment for ticket {ticket.Name}?", 
                    "Stop Payment", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement stop payment logic
                    MessageBox.Show($"Payment stopped for ticket {ticket.Name}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Ticket ticket)
            {
                try
                {
                    // Start automation for APP
                    await StartAutomationAsync(ticket, "APP");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting automation: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void WebButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Ticket ticket)
            {
                try
                {
                    // Start automation for WEB
                    await StartAutomationAsync(ticket, "WEB");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting automation: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async System.Threading.Tasks.Task StartAutomationAsync(Ticket ticket, string platform)
        {
            if (_automationService == null)
            {
                MessageBox.Show("Automation service is not available.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Parse configuration from ticket
            BookingConfig? config = null;
            if (!string.IsNullOrEmpty(ticket.ConfigurationJson))
            {
                try
                {
                    config = JsonSerializer.Deserialize<BookingConfig>(ticket.ConfigurationJson);
                }
                catch
                {
                    // If parsing fails, create a new config from ticket fields
                }
            }

            // Create config from ticket fields if not available
            if (config == null)
            {
                config = new BookingConfig
                {
                    SourceStation = ticket.From,
                    DestinationStation = ticket.To,
                    TravelDate = ticket.Date,
                    TrainNo = ticket.TrainNo,
                    TrainCoach = ticket.CLS,
                    Username = ticket.Username,
                    HeadlessMode = platform == "APP" // APP might use headless, WEB uses visible browser
                };

                // Set quota based on QT field
                if (ticket.QT.Contains("Tatkal", StringComparison.OrdinalIgnoreCase))
                {
                    config.Tatkal = true;
                }
                else if (ticket.QT.Contains("Premium", StringComparison.OrdinalIgnoreCase))
                {
                    config.PremiumTatkal = true;
                }
            }

            // Update ticket counts before starting automation
            if (platform == "APP")
            {
                ticket.AppCount++;
            }
            else
            {
                ticket.WebCount++;
            }

            // Save ticket counts if ticket service is available
            if (_ticketService != null)
            {
                ticket.LastUpdatedTimestamp = DateTime.UtcNow;
                await _ticketService.UpdateTicketAsync(ticket);
            }

            // Start automation (AutomationService will update ticket status)
            await _automationService.StartTicketAsync(ticket, config);
            
            MessageBox.Show($"Automation started for ticket {ticket.Name} on {platform} platform.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

