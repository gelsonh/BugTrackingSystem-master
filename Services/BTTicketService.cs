using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BugTrackingSystem.Services
{
    public class BTTicketService : IBTTicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        public BTTicketService(ApplicationDbContext context, UserManager<BTUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task AddTicketAsync(Ticket? ticket)
        {
            try
            {
                if (ticket != null)
                {
                    await _context.AddAsync(ticket);
                    await _context.SaveChangesAsync();
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task AddTicketAttachmentAsync(TicketAttachment? ticketAttachment)
        {
            try
            {
                if (ticketAttachment != null)
                {
                    await _context.AddAsync(ticketAttachment);

                }
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task AddTicketCommentAsync(TicketComment? ticketComment)
        {
            try
            {
                if (ticketComment != null)
                {
                    _context.TicketComments.Add(ticketComment);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task ArchiveTicketAsync(Ticket? ticket)
        {
            if (ticket != null)
            {
                ticket.Archived = true;
                _context.Tickets.Update(ticket);
                await _context.SaveChangesAsync();
            }
        }


        public async Task AssignTicketAsync(int? ticketId, string? userId)
        {
            try
            {
                if (ticketId != null && !string.IsNullOrEmpty(userId))
                {
                    BTUser? btUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    Ticket? ticket = await GetTicketByIdAsync(ticketId, btUser!.CompanyId);

                    if (ticket != null && btUser != null)
                    {
                        ticket!.DeveloperUserId = userId;
                        //TODO: Set ticket Status to "Development" with LookupService

                        await UpdateTicketAsync(ticket);
                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<Ticket>> GetAllTicketsByCompanyIdAsync(int? companyId)
        {
            try
            {
                List<Ticket> tickets = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.History)
                    .SelectMany(p => p.Tickets)
                    .Include(t => t.Project)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .Include(t => t.TicketStatus)
                    .ToListAsync();

                return tickets;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<Ticket> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets
                    .Include(t => t.Project)
                           .ThenInclude(p => p!.Company)
                    .Include(t => t.Attachments)
                    .Include(t => t.Comments)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.History)
                    .Include(t => t.SubmitterUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId && t.Archived == false);

                return ticket!;
            }

            catch (Exception)
            {

                throw;
            }
        }

        public async Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int? ticketAttachmentId)
        {
            try
            {
                TicketAttachment? ticketAttachment = await _context.TicketAttachments
                                                                  .Include(t => t.BTUser)
                                                                  .FirstOrDefaultAsync(t => t.Id == ticketAttachmentId);
                return ticketAttachment;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(int? ticketId, int? companyId)
        {

            try
            {

                Ticket? ticket = new();

                if (ticketId != null && companyId != null)
                {
                    ticket = await _context.Tickets
                    .Where(t => t.Project!.CompanyId == companyId && t.Archived == false)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.Project)
                             .ThenInclude(p => p!.Company)
                    .Include(t => t.History)
                    .Include(t => t.Comments)
                    .Include(t => t.Attachments)
                    .Include(t => t.SubmitterUser)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);


                }
                return ticket;

            }
            catch (Exception)
            {
                throw;
            }
        }


        public Task<BTUser?> GetTicketDeveloperAsync(int? ticketId, int? companyId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<Ticket>> GetTicketsByUserIdAsync(string? userId, int? companyId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TicketType>> GetTicketTypesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task RestoreTicketAsync(Ticket? ticket)
        {
            if (ticket != null)
            {
                try
                {
                    ticket.Archived = false;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error trying to restore the ticket", ex);
                }
            }
        }



        public async Task UpdateTicketAsync(Ticket? ticket)
        {
            try
            {
                if (ticket != null)
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
