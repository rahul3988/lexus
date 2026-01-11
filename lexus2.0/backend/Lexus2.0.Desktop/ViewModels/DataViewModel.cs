using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for data view with real-time updates
    /// </summary>
    public class DataViewModel : INotifyPropertyChanged
    {
        private readonly TicketService _ticketService;
        private bool _isLoading;

        public DataViewModel(TicketService ticketService)
        {
            _ticketService = ticketService;
            Tickets = new ObservableCollection<Ticket>();
        }

        public ObservableCollection<Ticket> Tickets { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadTicketsAsync()
        {
            IsLoading = true;
            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync();
                Tickets.Clear();
                foreach (var ticket in tickets)
                {
                    Tickets.Add(ticket);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void UpdateTicket(Ticket ticket)
        {
            var existingTicket = GetTicketById(ticket.TicketId);
            if (existingTicket != null)
            {
                var index = Tickets.IndexOf(existingTicket);
                Tickets[index] = ticket;
            }
            else
            {
                Tickets.Insert(0, ticket);
            }
        }

        private Ticket? GetTicketById(string ticketId)
        {
            foreach (var ticket in Tickets)
            {
                if (ticket.TicketId == ticketId)
                {
                    return ticket;
                }
            }
            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

