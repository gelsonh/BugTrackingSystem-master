using BugTrackingSystem.Models;

namespace BugTrackingSystem.Services.Interfaces
{
    public interface IBTTicketService
    {
       
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
        public Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync();
        public Task<IEnumerable<TicketType>> GetTicketTypesAsync();

        public Task ArchiveTicketAsync(Ticket? ticket);
        public Task RestoreTicketAsync(Ticket? ticket);
    }
}
