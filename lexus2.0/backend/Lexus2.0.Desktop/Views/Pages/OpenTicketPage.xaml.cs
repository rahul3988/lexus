using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;
using Lexus2_0.Desktop.Views;

namespace Lexus2_0.Desktop.Views.Pages
{
    public partial class OpenTicketPage : UserControl
    {
        private TicketService? _ticketService;
        private AutomationService? _automationService;
        private ObservableCollection<Ticket> _tickets = new();

        public TicketService? TicketService
        {
            get => _ticketService;
            set
            {
                _ticketService = value;
                LoadTickets();
            }
        }

        public AutomationService? AutomationService
        {
            get => _automationService;
            set => _automationService = value;
        }

        public OpenTicketPage()
        {
            InitializeComponent();
            TicketHistoryGrid.ItemsSource = _tickets;
        }

        private async void LoadTickets()
        {
            if (_ticketService == null) return;

            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync();
                _tickets.Clear();
                foreach (var ticket in tickets)
                {
                    _tickets.Add(ticket);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tickets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAllTicketButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tickets.Count == 0)
            {
                MessageBox.Show("No tickets available to open.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenAllTicketDialog(_tickets.ToList(), _automationService, _ticketService);
            dialog.ShowDialog();
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tickets.Count == 0)
            {
                MessageBox.Show("No tickets to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete all {_tickets.Count} tickets?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteAllTickets();
            }
        }

        private async void DeleteAllTickets()
        {
            if (_ticketService == null) return;

            try
            {
                foreach (var ticket in _tickets.ToList())
                {
                    await _ticketService.DeleteTicketAsync(ticket.TicketId);
                }
                _tickets.Clear();
                MessageBox.Show("All tickets deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting tickets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenTicketButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Ticket ticket)
            {
                var dialog = new OpenAllTicketDialog(new System.Collections.Generic.List<Ticket> { ticket }, _automationService, _ticketService);
                dialog.ShowDialog();
            }
        }

        private void EditTicketButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Ticket ticket)
            {
                MessageBox.Show($"Edit ticket {ticket.TicketId} functionality - To be implemented.", 
                    "Edit Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteTicketButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Ticket ticket)
            {
                var result = MessageBox.Show($"Are you sure you want to delete ticket {ticket.Name}?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes && _ticketService != null)
                {
                    try
                    {
                        await _ticketService.DeleteTicketAsync(ticket.TicketId);
                        _tickets.Remove(ticket);
                        MessageBox.Show("Ticket deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

