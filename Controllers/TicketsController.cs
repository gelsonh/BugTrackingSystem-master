using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using Microsoft.AspNetCore.Identity;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using BugTrackingSystem.Models.ViewModels;
using BugTrackingSystem.Models.Enums;
using BugTrackingSystem.Extensions;
using Org.BouncyCastle.Tls;

namespace BugTrackingSystem.Controllers
{

    [Authorize]
    public class TicketsController : BTBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTTicketService _ticketService;
        private readonly IBTFileService _fileService;
        private readonly IBTProjectService _projectService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTNotificationService _notificationService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTTicketService ticketService, IBTFileService fileService, IBTProjectService projectService, IBTRolesService rolesService, IBTTicketHistoryService ticketHistoryService, IBTNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _ticketService = ticketService;
            _fileService = fileService;
            _projectService = projectService;
            _rolesService = rolesService;
            _ticketHistoryService = ticketHistoryService;
            _notificationService = notificationService;
        }



        [Authorize(Roles = "Admin")]
        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            IEnumerable<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyIdAsync(_companyId);

            
            tickets = tickets.Where(t => !t.Archived);

            if (tickets != null)
            {
                ViewBag.TicketCount = tickets.Count(); 
                ViewBag.NewCount = tickets.Count(t => t.TicketStatus!.Name == "New"); 
                ViewBag.DevelopmentCount = tickets.Count(t => t.TicketStatus!.Name == "Development");
                ViewBag.TestingCount = tickets.Count(t => t.TicketStatus!.Name == "Testing");
                ViewBag.ResolvedCount = tickets.Count(t => t.TicketStatus!.Name == "Resolved");
            }
            else
            {
                ViewBag.TicketCount = 0; 
            }
            return View(tickets); 
        }




        [HttpGet]
        public async Task<IActionResult> AssignTicket(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AssignTicketViewModel viewModel = new();

            viewModel.Ticket = await _ticketService.GetTicketByIdAsync(id, _companyId);
            string? currentDeveloper = viewModel.Ticket?.DeveloperUserId;
            viewModel.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(viewModel.Ticket?.ProjectId, nameof(BTRoles.Developer), _companyId), "Id", "FullName", currentDeveloper);


            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(AssignTicketViewModel viewModel)
        {
            if (viewModel.DeveloperId != null && viewModel.Ticket?.Id != null)
            {
                try
                {
                    await _ticketService.AssignTicketAsync(viewModel.Ticket?.Id, viewModel.DeveloperId);
                    return RedirectToAction(nameof(Details), new { id = viewModel.Ticket?.Id });
                }
                catch (Exception)
                {

                    throw;
                }

                // TODO: Add History

                // TODO: Add Notification
            }

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketComment([Bind("Comment,TicketId")] TicketComment ticketComment, int ticketId)
        {
            if (ModelState.IsValid)
            {
                BTUser? user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    ticketComment.UserId = user.Id;
                }
                ticketComment.Created = DateTime.Now;

                await _ticketService.AddTicketCommentAsync(ticketComment);

                return RedirectToAction("Details", new { id = ticketId });
            }

      
            return View(ticketComment);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;

            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTimeOffset.Now.UtcDateTime;
                ticketAttachment.BTUserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";
            }
            else
            {
                statusMessage = "Error: Invalid data.";

            }

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
        }

        [HttpGet]
        public async Task<IActionResult> AssignDeveloper(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Id == ticket.ProjectId), "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Id", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Id", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Id", ticket.TicketTypeId);

            return View(ticket);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDeveloper(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }
;
            ModelState.Remove("SubmitterUserId");

            if (ModelState.IsValid)
            {

                string? userId = _userManager.GetUserId(User);
                Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, _companyId);

                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Get a snapshot of the new ticket data after updating
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, _companyId);

                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, userId);
                await _notificationService.NewDeveloperNotificationAsync(ticket.Id, ticket.DeveloperUserId, userId);
                return RedirectToAction(nameof(Index));
            }

            ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Id == ticket.ProjectId), "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Id", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Id", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Id", ticket.TicketTypeId);

            return View(ticket);
        }


        public async Task<IActionResult> ShowFile(int id)
        {
            TicketAttachment? ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
            string? fileName = ticketAttachment!.FileName;
            byte[]? fileData = ticketAttachment.FileData;
            string ext = Path.GetExtension(fileName!).Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData!, $"application/{ext}");
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? Id)
        {
            if (Id == null &&  _context.Tickets == null)
            {
                return NotFound();
            }

            Ticket? ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.History)
                .Include(t => t.TicketType)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)  // Load the User associated with each Comment
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(m => m.Id == Id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {


            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.CompanyId == _companyId), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name");
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId")] Ticket ticket)
        {
            string? userId = _userManager.GetUserId(User);

            ModelState.Remove("SubmitterUserId");

            if (ModelState.IsValid)
            {
                ticket.SubmitterUserId = userId;

                ticket.Created = DateTime.Now;



                // Add the service 
                await _ticketService.AddTicketAsync(ticket);

                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket?.Id, _companyId);

                if (newTicket != null)
                {
                    await _ticketHistoryService.AddHistoryAsync(null, newTicket, userId);
                }

                await _notificationService.NewTicketNotificationAsync(ticket?.Id, userId);

                return RedirectToAction("Details", "Projects", new { id = ticket!.ProjectId });
            }

            ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "FullName", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Id == ticket.ProjectId), "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Users, "Id", "FullName", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Name", ticket.TicketStatus?.Id);
            ViewData["TicketTypesId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);

            return View(ticket);
        }




        // GET: Tickets/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

          
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", ticket.ProjectId);
            
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);

            return View(ticket);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            string? userId = _userManager.GetUserId(User);

            ModelState.Remove("SubmitterUserId");

            if (ModelState.IsValid)
            {

                ticket.SubmitterUserId = userId;
                Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, _companyId);

                ticket.Updated = DateTime.Now;

                // Update the service 
                await _ticketService.UpdateTicketAsync(ticket);

                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, _companyId);

                if (newTicket != null)
                {
                    await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, userId);
                }

                await _notificationService.NewTicketNotificationAsync(ticket.Id, userId);

                return RedirectToAction("Details", "Projects", new { id = ticket.ProjectId });
            }

            ViewData["DeveloperUserId"] = new SelectList(_context.Users, "Id", "FullName", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Id == ticket.ProjectId), "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Users, "Id", "FullName", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus, "Id", "Name", ticket.TicketStatus?.Id);
            ViewData["TicketTypesId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);

            return View(ticket);
        }




        [HttpGet]
        public async Task<IActionResult> TicketsArchive()
        {
            List<Ticket> archivedTickets = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .Where(t => t.Archived)
                .ToListAsync();

            return View(archivedTickets);
        }


        [HttpPost]
        public async Task<IActionResult> ArchiveTicket(int id)
        {
            Ticket? ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            await _ticketService.ArchiveTicketAsync(ticket);

            return RedirectToAction("Index", new { id = ticket.Id });
        }


        [HttpPost]
        public async Task<IActionResult> RestoreTicket(int id)
        {
            Ticket? ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Archived = false;
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = ticket.Id });
        }


        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tickets == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Tickets'  is null.");
            }
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Restore(int id)
        {
            Ticket? ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                await _ticketService.RestoreTicketAsync(ticket);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
            return (_context.Tickets?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
