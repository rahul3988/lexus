using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages.Options
{
    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Status { get; set; } = "";
        public string Details { get; set; } = "";
    }

    public partial class ShowHistoryPage : UserControl
    {
        private TicketService? _ticketService;
        private ObservableCollection<HistoryEntry> _history = new();

        public TicketService? TicketService
        {
            get => _ticketService;
            set
            {
                _ticketService = value;
                LoadHistory();
            }
        }

        public ShowHistoryPage()
        {
            InitializeComponent();
            HistoryGrid.ItemsSource = _history;
        }

        private async void LoadHistory()
        {
            if (_ticketService == null) return;

            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync();
                _history.Clear();

                foreach (var ticket in tickets.OrderByDescending(t => t.CreatedTimestamp))
                {
                    _history.Add(new HistoryEntry
                    {
                        Timestamp = ticket.CreatedTimestamp,
                        Action = $"Ticket {ticket.TicketId}",
                        Status = ticket.Status.ToString(),
                        Details = $"Attempts: {ticket.AttemptCount}, Success: {ticket.SuccessCount}, Failures: {ticket.FailureCount}"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear history? This action cannot be undone.",
                "Clear History", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _history.Clear();
                MessageBox.Show("History cleared", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

