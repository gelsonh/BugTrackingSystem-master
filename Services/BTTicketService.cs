using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugTrackingSystem.Services
{
    public class BTTicketService : IBTTicketService
    {
        // Private readonly field for accessing the application database context
        private readonly ApplicationDbContext _context;

        // Private readonly field for managing user-related operations
        private readonly UserManager<BTUser> _userManager;

        // Constructor for the BTTicketService class
        public BTTicketService(ApplicationDbContext context, UserManager<BTUser> userManager)
        {
            // Assign the injected database context to the private field 
            _context = context;

            // Assign the injected user manager to the private field
            _userManager = userManager;
        }


        public async Task AddTicketAsync(Ticket? ticket)
        {
            if (ticket != null)
            {

                await _context.AddAsync(ticket);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddTicketAttachmentAsync(TicketAttachment? ticketAttachment)
        {
            try
            {
                await _context.AddAsync(ticketAttachment!);

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
        
        // Initialize a new Ticket object
        Ticket? ticket = null;

        // Check if both ticketId and companyId are provided
        if (ticketId is not 0 && companyId is not 0)
        {
            // Query the database for the ticket with the specified ID and company ID
            // Includes related entities 
            ticket = await _context.Tickets
                .Where(t => t.Project!.CompanyId == companyId && t.Archived == false)
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Company)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .Include(t => t.History)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .Include(t => t.SubmitterUser)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
        }

        // Return the retrieved ticket (or null if not found)
        return ticket;
    }
    catch (Exception)
    {
        // Handle exceptions by rethrowing them
        throw;
    }
}


        public async Task<BTUser?> GetTicketDeveloperAsync(int? ticketId, int? companyId)
        {
            try
            {
                if (ticketId == null)
                {
                    return null;
                }

                Ticket? ticket = await _context.Tickets.FindAsync(ticketId);

                if (ticket == null)
                {
                    return null;
                }

                Project? project = await _context.Projects.FindAsync(ticket.ProjectId);

                if (companyId != null && project?.CompanyId != companyId)
                {
                    return null;
                }

                BTUser? developer = await _userManager.FindByIdAsync(ticket.DeveloperUserId!);

                return developer;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        public async Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync()
        {
            return await _context.TicketPriorities.ToListAsync();
        }


        public async Task<List<Ticket>> GetTicketsByUserIdAsync(string? userId, int? companyId)
        {
            try
            {
                if (userId != null && companyId != null)
                {
                    return new List<Ticket>();
                }

                return await _context.Tickets
                    .Where(t => t.SubmitterUserId == userId && t.Project!.CompanyId == companyId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Devuelve una lista vacía si ocurre una excepción
                return new List<Ticket>();
            }
        }


        public async Task<IEnumerable<TicketStatus>> GetTicketStatusAsync()
        {
            return await _context.TicketStatus.ToListAsync();
        }


        public async Task<IEnumerable<TicketType>> GetTicketTypesAsync()
        {
            return await _context.TicketTypes.ToListAsync();
        }


        public async Task<List<Ticket>> GetUnassignedTicketsAsync(int? companyId)
        {
            // Obtén el DbContext a través de la inyección de dependencias
            var context = _context;

            // Busca los tickets que no están asignados y pertenecen a la compañía del usuario
            List<Ticket> unassignedTickets = await context.Tickets
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketStatus)
                .Where(t => t.DeveloperUserId == null && t.Project!.CompanyId == companyId)
                .ToListAsync();

            return unassignedTickets;
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
