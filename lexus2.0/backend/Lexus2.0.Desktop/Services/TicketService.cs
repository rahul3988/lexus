using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Models;

namespace Lexus2_0.Desktop.Services
{
    /// <summary>
    /// Service for managing tickets
    /// </summary>
    public class TicketService
    {
        private readonly DatabaseContext _dbContext;

        public TicketService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Ticket> CreateTicketAsync(Ticket ticket)
        {
            return await _dbContext.CreateTicketAsync(ticket);
        }

        public async Task<List<Ticket>> GetAllTicketsAsync()
        {
            return await _dbContext.GetAllTicketsAsync();
        }

        public async Task<Ticket?> GetTicketByIdAsync(string ticketId)
        {
            return await _dbContext.GetTicketByIdAsync(ticketId);
        }

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            ticket.LastUpdatedTimestamp = DateTime.UtcNow;
            await _dbContext.UpdateTicketAsync(ticket);
        }

        public async Task DeleteTicketAsync(string ticketId)
        {
            await _dbContext.DeleteTicketAsync(ticketId);
        }

        public async Task RetryTicketAsync(string ticketId)
        {
            var ticket = await GetTicketByIdAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = TicketStatus.Pending;
                ticket.ErrorMessage = null;
                await UpdateTicketAsync(ticket);
            }
        }
    }
}

