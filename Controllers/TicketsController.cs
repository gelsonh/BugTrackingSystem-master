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


namespace BugTrackingSystem.Controllers
{

    [Authorize]
    public class TicketsController : BTBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTCompanyService _companyService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTFileService _fileService;
        private readonly IBTProjectService _projectService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTNotificationService _notificationService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTCompanyService companyService, IBTTicketService ticketService, IBTFileService fileService, IBTProjectService projectService, IBTRolesService rolesService, IBTTicketHistoryService ticketHistoryService, IBTNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _companyService = companyService;
            _ticketService = ticketService;
            _fileService = fileService;
            _projectService = projectService;
            _rolesService = rolesService;
            _ticketHistoryService = ticketHistoryService;
            _notificationService = notificationService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            IEnumerable<Ticket> tickets = new List<Ticket>();

            if (await _userManager.IsInRoleAsync(user!, "Admin"))
            {
                tickets = await _ticketService.GetAllTicketsByCompanyIdAsync(_companyId);

                ViewBag.TicketCount = tickets.Count();
                ViewBag.NewCount = tickets.Count(t => t.TicketStatus?.Name == "New");
                ViewBag.DevelopmentCount = tickets.Count(t => t.TicketStatus?.Name == "Development");
                ViewBag.TestingCount = tickets.Count(t => t.TicketStatus?.Name == "Testing");
                ViewBag.ResolvedCount = tickets.Count(t => t.TicketStatus?.Name == "Resolved");
            }
            else
            {
                if (await _userManager.IsInRoleAsync(user!, "Developer") || await _userManager.IsInRoleAsync(user!, "ProjectManager"))
                {
                    tickets = await _context.Tickets.Where(t => t.DeveloperUserId == user!.Id && t.Project!.CompanyId == _companyId).ToListAsync();
                }

            }
            return View(tickets);
        }

        [Authorize(Roles = "Admin, ProjectManager, DemoUser")]
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

        [Authorize(Roles = "Admin, ProjectManager, DemoUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(AssignTicketViewModel viewModel)
        {
            if (viewModel.DeveloperId != null && viewModel.Ticket?.Id != null)
            {
                // Guarda una copia del ticket antiguo antes de la actualización
                Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(viewModel.Ticket?.Id, _companyId);

                try
                {
                    await _ticketService.AssignTicketAsync(viewModel.Ticket?.Id, viewModel.DeveloperId);
                }
                catch (Exception)
                {
                    throw;
                }

                // Obtiene una instantánea de los nuevos datos del ticket después de la actualización
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(viewModel.Ticket?.Id, _companyId);

                // Añade el historial
                string? userId = _userManager.GetUserId(User);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, userId);

                // Añade la notificación
                await _notificationService.NewDeveloperNotificationAsync(viewModel.Ticket?.Id, viewModel.DeveloperId, userId);

                return RedirectToAction(nameof(Details), new { id = viewModel.Ticket?.Id });
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

        [Authorize(Roles = "Admin, ProjectManager, DemoUser")]
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
                .Include(t => t.History)
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

        [Authorize(Roles = "Admin, ProjectManager, DemoUser")]
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

        public async Task<IActionResult> UnassignedTickets()
        {
            // Obtén el companyId del usuario que inició sesión
            int? companyId = _userManager.GetUserAsync(User).Result?.CompanyId;

            List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

            // Obtén el número de tickets no asignados
            int unassignedTicketCount = tickets.Count;

            // Puedes pasar este número a la vista a través de ViewBag o ViewData
            ViewBag.UnassignedTicketCount = unassignedTicketCount;

            return View(tickets);
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
            if (Id == null && _context.Tickets == null)
            {
                return NotFound();
            }

            Ticket? ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .Include(t => t.History)
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



        public async Task<IActionResult> Create()
        {
            string? userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");

            int? companyId = int.Parse(User.Claims.First(c => c.Type == "CompanyId").Value);

            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);

            ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes.ToList(), "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities.ToList(), "Id", "Name");
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatus.ToList(), "Id", "Name");

            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketPriorityId,DeveloperUserId")] Ticket ticket)
        {
            string? userId = _userManager.GetUserId(User);

            ModelState.Remove("SubmitterUserId");

            if (ModelState.IsValid)
            {
                ticket.SubmitterUserId = userId;
                ticket.Created = DateTime.Now;

                var defaultStatus = await _context.TicketStatus.FirstOrDefaultAsync();
                if (defaultStatus != null)
                {
                    ticket.TicketStatusId = defaultStatus.Id;
                }

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

            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _ticketService.GetTicketStatusAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

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

                // Save changes to the database
                _context.Update(ticket);
                await _context.SaveChangesAsync();

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

            // Obtén el número de tickets archivados
            int archivedTicketCount = archivedTickets.Count;

            // Puedes pasar este número a la vista a través de ViewBag o ViewData
            ViewBag.ArchivedTicketCount = archivedTicketCount;

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



