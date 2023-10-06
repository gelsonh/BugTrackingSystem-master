using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using Microsoft.AspNetCore.Identity;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using BugTrackingSystem.Models.ViewModels;
using BugTrackingSystem.Models.Enums;

namespace BugTrackingSystem.Controllers
{
    public class ProjectsController : BTBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTProjectService _projectService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTNotificationService _notificationService;
        private readonly IBTFileService _fileService;



        public ProjectsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTProjectService projectService, IBTRolesService rolesService, IBTTicketService ticketService, IBTTicketHistoryService ticketHistoryService, IBTNotificationService notificationService, IBTFileService fileService)
        {
            _context = context;
            _userManager = userManager;
            _projectService = projectService;
            _rolesService = rolesService;
            _ticketService = ticketService;
            _ticketHistoryService = ticketHistoryService;
            _notificationService = notificationService;
            _fileService = fileService;
        }

        // GET: Projects
        [Authorize] 
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            IEnumerable<Project>? projects;

            if (roles.Contains("Admin") || roles.Contains("ProjectManager"))
            {
                
                projects = await _projectService.GetAllProjectsByCompanyIdAsync(_companyId);
            }
            else
            {
   
                projects = await _projectService.GetUserProjectsAsync(user!.Id);
            }

            projects = projects!.Where(p => !p.Archived);

            // Obtén el número de proyectos
            int projectCount = projects.Count();

            // Puedes pasar este número a la vista a través de ViewBag o ViewData
            ViewBag.ProjectCount = projectCount;

            return View(projects);
        }


        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> AssignPM(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Project? project = await _projectService.GetProjectByIdAsync(id, _companyId);

            if (project == null)
            {
                return NotFound();
            }

            // Get the list project managers
            IEnumerable<BTUser> projectManagers = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(id);

            AssignPMViewModel viewModel = new()
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id),
                PMId = currentPM?.Id

            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.PMId))
            {

                if (await _projectService.AddProjectManagerAsync(viewModel.PMId, viewModel.ProjectId))
                {
                    return RedirectToAction(nameof(Details), new { id = viewModel.ProjectId });

                }
                else
                {
                    ModelState.AddModelError("PMId", "Error assigning PM.");
                }
                ModelState.AddModelError("PMId", "No Project Manager chosen. Please select a PM.");

            }

            IEnumerable<BTUser> projectManagers = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(viewModel.ProjectId);
            viewModel.PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id);



            return View(viewModel);
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> AssignProjectMembers(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Project? project = await _projectService.GetProjectByIdAsync(id, _companyId);

            if (project == null)
            {
                return NotFound();
            }

            List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.Submitter), _companyId);
            List<BTUser> developer = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.Developer), _companyId);

            List<BTUser> usersList = submitters.Concat(developer).ToList();

            IEnumerable<string> currentMembers = project.Members.Select(u => u.Id);

            ProjectMembersViewModel viewModel = new()
            {
                Project = project,
                UsersList = new MultiSelectList(usersList, "Id", "FullName", currentMembers)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProjectMembers(ProjectMembersViewModel viewModel)
        {
            if (viewModel.SelectedMembers != null)
            {
                await _projectService.RemoveMembersFromProjectAsync(viewModel.Project?.Id, _companyId);

                await _projectService.AddMembersToProjectAsync(viewModel.SelectedMembers, viewModel.Project?.Id, _companyId);

                return RedirectToAction(nameof(Details), new { id = viewModel.Project?.Id });
            }
            //Handle the error
            ModelState.AddModelError("SelectedMembers", "No Users chosen. Please selectUsers!");


            viewModel.Project = await _projectService.GetProjectByIdAsync(viewModel.Project?.Id, _companyId);
            List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.Submitter), _companyId);
            List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.Developer), _companyId);

            List<BTUser> usersList = submitters.Concat(developers).ToList();

            IEnumerable<string> currentMembers = viewModel.Project!.Members.Select(u => u.Id);
            viewModel.UsersList = new MultiSelectList(usersList, "Id", "FullName", currentMembers);



            return View(viewModel);
        }


        public async Task<IActionResult> UnassignedProjects()
        {
            // Obtén el companyId del usuario que inició sesión
            int? companyId = _userManager.GetUserAsync(User).Result?.CompanyId;

            List<Project> projects = await _projectService.GetUnassignedProjectsAsync(companyId);

            // Obtén el número de proyectos no asignados
            int unassignedProjectCount = projects.Count;

            // Puedes pasar este número a la vista a través de ViewBag o ViewData
            ViewBag.UnassignedProjectCount = unassignedProjectCount;

            return View(projects);
        }


        [Authorize(Roles = "Admin, ProjectManager")]
        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            Project? project = await _context.Projects
               
                .Include(p => p.ProjectPriority)  // Include the related ProjectPriority entity
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketStatus)  // Include the related TicketStatus entity for each Ticket
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketPriority)  // Include the related TicketPriority entity for each Ticket
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.History)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _companyId);

            if (project == null)
            {
                return NotFound($"No project found with ID {id} for company {_companyId}.");
            }

            return View(project);
        }




        [Authorize(Roles = "Admin, ProjectManager")]
        // GET: Projects/Create
        public IActionResult Create()
        {

            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name");

            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name");

        

            //// Si el método fue exitoso, puedes obtener la lista de product managers de esta manera
            //ViewData["PMId"] = new SelectList(_context.Users.Where(u => u.Roles.Any(r => r.RoleId == "ProjectManager")), "Id", "Name");
            

            return View();
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Created,StartDate,EndDate,ProjectPriorityId,ImageFileData,ImageFileType,Archived")] Project project)
        {
            string? userId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                // Obtén el CompanyId del usuario que inició sesión
                var user = await _userManager.FindByIdAsync(userId!);
                project.CompanyId = user!.CompanyId;

                project.Created = DateTime.Now;

                if (project.ImageFormFile != null)
                {
                    using var memoryStream = new MemoryStream();
                    await project.ImageFormFile.CopyToAsync(memoryStream);
                    project.ImageFileData = memoryStream.ToArray();
                }

                await _projectService.AddProjectAsync(project);

                if (project?.Id > 0)
                {
                    await _notificationService.NewProjectNotificationAsync(project.Id, userId);
                    return RedirectToAction("Details", "Projects", new { id = project.Id });
                }
                else
                {
                    ModelState.AddModelError("", "El proyecto no se guardó correctamente.");
                    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", project!.CompanyId);
                    ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
                    return View(project);
                }
            }

            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", project.CompanyId);
            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);

            return View(project);
        }




        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {

            Project? project = await _context.Projects.FindAsync(id);


            if (project == null)
            {
                return NotFound();
            }


            if (project.CompanyId != _companyId)
            {
                return Unauthorized();
            }

            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", project.CompanyId);
            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name", project.ProjectPriorityId);

            return View(project);
        }

        // POST: Projects/Edit/5
        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Created,StartDate,EndDate,ProjectPriorityId,ImageFileData,ImageFileType,Archived")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            string? userId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {


                // Update the service 
                await _projectService.UpdateProjectAsync(project);

                Project? newProject = await _projectService.GetProjectAsNoTrackingAsync(project.Id, _companyId);

               

                await _notificationService.NewProjectNotificationAsync(project.Id, userId);

                return RedirectToAction("Details", "Projects", new { id = project.Id });
            }

            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }


        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> ProjectsArchive()
        {
            List<Project> archivedProjects = await _context.Projects
                .Include(p => p.Tickets)
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)        
                .Where(p => p.Archived)
                .ToListAsync();

            // Obtén el número de proyectos archivados
            int archivedProjectCount = archivedProjects.Count;

            // Puedes pasar este número a la vista a través de ViewBag o ViewData
            ViewBag.ArchivedProjectCount = archivedProjectCount;

            return View(archivedProjects);
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        public async Task<IActionResult> ArchiveProject(int id)
        {
            Project? project = await _context.Projects.Include(p => p.Tickets).FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _companyId);
            if (project == null)
            {
                return NotFound();
            }

            project.Archived = true;

            foreach (var ticket in project.Tickets)
            {
                ticket.Archived = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        public async Task<IActionResult> RestoreProject(int id)
        {
            Project? project = await _context.Projects.Include(p => p.Tickets).FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _companyId);
            if (project == null)
            {
                return NotFound();
            }

            project.Archived = false;

            foreach (var ticket in project.Tickets)
            {
                ticket.Archived = false;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }





        // POST: Projects/Archive/5
        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Projects'  is null.");
            }
            Project? project = await _context.Projects.FindAsync(id);
            if (project != null)
            {

                await _projectService.ArchiveProjectAsync(project, project.CompanyId);
            }

            return RedirectToAction(nameof(Index));
        }
      

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
