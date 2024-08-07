﻿using BugTrackingSystem.Models;

namespace BugTrackingSystem.Services.Interfaces
{

    // An interface defines a contract that classes can implement
    // It contains method signatures without implementations
    public interface IBTTicketService
    {
       
        // Method asynchronously
        public Task AddTicketAsync(Ticket? ticket);
        public Task AssignTicketAsync(int? ticketId, string? userId);
        public Task AddTicketAttachmentAsync(TicketAttachment? ticketAttachment);
        public Task AddTicketCommentAsync(TicketComment? ticketComment);

        public Task UpdateTicketAsync(Ticket? ticket);
        public Task<List<Ticket>> GetAllTicketsByCompanyIdAsync(int? companyId);
        public Task<Ticket> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId);
        public Task<Ticket?> GetTicketByIdAsync(int? ticketId, int? companyId);
        public Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int? ticketAttachmentId);
        public Task<BTUser?> GetTicketDeveloperAsync(int? ticketId, int? companyId);

        public Task<List<Ticket>> GetTicketsByUserIdAsync(string? userId, int? companyId);
        public Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync();
        public Task<IEnumerable<TicketStatus>> GetTicketStatusAsync();
        public Task<IEnumerable<TicketType>> GetTicketTypesAsync();
        public Task<List<Ticket>> GetUnassignedTicketsAsync(int? companyId);


        public Task ArchiveTicketAsync(Ticket? ticket);
        public Task RestoreTicketAsync(Ticket? ticket);
    }
}
