using BugTrackingSystem.Controllers;
using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using BugTrackingSystem.Models.Enums;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace BugTrackingSystem.Services
{

    public class BTProjectService : IBTProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ILogger<BTProjectService> _logger;
        private readonly IBTRolesService _rolesService;



        public BTProjectService(ApplicationDbContext context, UserManager<BTUser>  userManager, ILogger<BTProjectService> logger, IBTRolesService rolesService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _rolesService = rolesService;
          


        }

        public Task AssignDefautProjectManagersAsync(ApplicationDbContext context)
        {
            throw new NotImplementedException();
        }

        public async Task AddMembersToProjectAsync(IEnumerable<string>? userId, int? projectId, int? companyId)
        {
            try
            {
                if (userId != null)
                {
                    Project? project = await GetProjectByIdAsync(projectId, companyId);

                    foreach (string user in userId)
                    {
                        BTUser? btUser = await _context.Users.FindAsync(user);


                        if (project != null && btUser != null)
                        {
                            bool IsOnProject = project.Members.Any(m => m.Id == user);
                            if (!IsOnProject)
                            {
                                project.Members.Add(btUser);
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId)
        {
            try
            {
                if (member != null && projectId != null)
                {
                    Project? project = await GetProjectByIdAsync(projectId, member.CompanyId);

                    if (project != null)
                    {
                        // project instance must ?Include Members to do the following
                        bool IsOnProject = project.Members.Any(m => m.Id == member.Id);

                        if (!IsOnProject)
                        {
                            project.Members.Add(member);
                            await _context.SaveChangesAsync();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task AddProjectAsync(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            try
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.InnerException!.Message);
            }
        }






        public async Task<bool> AddProjectManagerAsync(string? userId, int? projectId)
        {
            try
            {
                if (userId != null && projectId != null)
                {
                    BTUser? currentPM = await GetProjectManagerAsync(projectId);
                    BTUser? selectedPM = await _context.Users.FindAsync(userId);

                    // Remove current PM
                    if (currentPM != null)
                    {
                        await RemoveProjectManagerAsync(projectId);

                    }

                    // Add new PM
                    try
                    {
                        await AddMemberToProjectAsync(selectedPM!, projectId);
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ArchiveProjectAsync(Project? project, int? companyId)
        {
            if (project != null && companyId != null && project.CompanyId == companyId)
            {
                try
                {
                    project.Archived = true;

                    IEnumerable<Ticket> tickets = _context.Tickets.Where(t => t.ProjectId == project.Id);

                    foreach (Ticket ticket in tickets)
                    {
                        ticket.ArchivedByProject = true;
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error trying to archive the project", ex);
                }
            }
        }


        public async Task<List<Project>> GetAllProjectsByCompanyIdAsync(int? companyId)
        {
            try
            {
                List<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.History)
                         .Include(p => p.ProjectPriority)
                    .ToListAsync();

                return projects;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int? companyId)
        {
            if (companyId != null)
            {
                List<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId && p.Archived)
                    .ToListAsync();

                return projects;
            }

            return new List<Project>();
        }


        public async Task<Project> GetProjectByIdAsync(int? projectId, int? companyId)
        {

            try
            {
                Project? project = new();

                if (projectId != null && companyId != null)
                {
                    project = await _context.Projects
                    .Include(p => p.Company)
                    .Include(p => p.Members)
                    .Include(p => p.ProjectPriority)
                    .Include(p => p.Tickets)
                            .ThenInclude(p => p.Comments)
                                  .ThenInclude(p => p.User)
                    .Include(p => p.Tickets)
                             .ThenInclude(p => p.Attachments)
                    .Include(p => p.Tickets)
                             .ThenInclude(p => p.History)
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);
                }


                return project!;
            }
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<BTUser> GetProjectManagerAsync(int? projectId)
        {
            try
            {
                Project? project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);

                if (project != null)
                {
                    foreach (BTUser member in project.Members)
                    {
                        if (await _rolesService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
                        {
                            return member;
                        }
                    }
                }
                return null!;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<BTUser>> GetProjectMembersByRoleAsync(int? projectId, string? roleName, int? companyId)
        {
            try
            {
                List<BTUser> members = new();

                if (projectId != null && companyId != null && !string.IsNullOrEmpty(roleName))
                {
                    Project? project = await GetProjectByIdAsync(projectId, companyId);

                    if (project != null)
                    {
                        foreach (BTUser member in project.Members)
                        {
                            if (await _rolesService.IsUserInRoleAsync(member, roleName))
                            {
                                members.Add(member);
                            }
                        }

                        // Testing purposes
                        List<string> developers = (await _rolesService.GetUsersInRoleAsync(roleName, companyId))?.Select(u => u.Id).ToList()!;

                        List<BTUser> users = project.Members.Where(m => developers.Contains(m.Id)).ToList();
                    }
                }

                return members;
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<IEnumerable<ProjectPriority>> GetProjectPrioritiesAsync()
        {
            return await _context.ProjectPriorities.ToListAsync();
        }

        public async Task<List<Project>?> GetUserProjectsAsync(string? userId)
        {
            if (userId == null)
            {
                return null;
            }

            try
            {
               
                BTUser? user = await _context.Users
                    .Include(u => u.Projects)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return null;
                }

              
                List<Project> userProjects = user.Projects.ToList();
                return userProjects;
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al obtener los proyectos del usuario.");
                return null;
            }
        }

        public async Task<List<Project>> GetUnassignedProjectsAsync(int? companyId)
        {
            // Obtén el DbContext a través de la inyección de dependencias
            var context = _context;

            // Busca los proyectos que no están asignados
            List<Project> unassignedProjects = await context.Projects
                .Include(p => p.Members)
                .Include(p => p.ProjectPriority)
                .Where(p => !p.Members.Any() && p.CompanyId == companyId)
                .ToListAsync();

            return unassignedProjects;
        }




        public async Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId)
        {
            try
            {
                if (member != null && projectId != null)
                {
                    Project? project = await GetProjectByIdAsync(projectId, member.CompanyId);

                    if (project != null)
                    {
                        bool IsOnProject = project.Members.Any(m => m.Id == member.Id);

                        if (IsOnProject)
                        {
                            project.Members.Remove(member);
                            await _context.SaveChangesAsync();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task RemoveMembersFromProjectAsync(int? projectId, int? companyId)
        {
            try
            {
                Project? project = await GetProjectByIdAsync(projectId, companyId);

                if (project != null)
                {
                    foreach (var member in project.Members)
                    {
                        if (!await _rolesService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
                        {
                            project.Members.Remove(member);
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task RemoveProjectManagerAsync(int? projectId)
        {
            try
            {
                if (projectId != null)
                {

                    Project? project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);

                    if (project != null)
                    {
                        foreach (BTUser member in project!.Members)
                        {
                            if (await _rolesService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
                            {
                                // Remove BTUser from Project
                                await RemoveMemberFromProjectAsync(member, projectId);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task RestoreProjectAsync(Project? project, int? companyId)
        {
            if (project != null && companyId != null && project.CompanyId == companyId)
            {
                try
                {
                    project.Archived = false;

                    IEnumerable<Ticket> tickets = _context.Tickets.Where(t => t.ProjectId == project.Id);

                    foreach (Ticket ticket in tickets)
                    {
                        ticket.ArchivedByProject = false;
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Here you can handle any error that might occur during the database update
                    throw new Exception("There was an error trying to restore the project", ex);
                }
            }
        }


        public async Task UpdateProjectAsync(Project? project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            try
            {
                var existingProject = await _context.Projects.FindAsync(project.Id);

                if (existingProject == null)
                {
                    throw new Exception("Project not found");
                }

                // Actualiza solo las propiedades que deseas cambiar
                existingProject.Name = project.Name;
                existingProject.Description = project.Description;
                existingProject.ProjectPriorityId = project.ProjectPriorityId;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



        public async Task<Project> GetProjectAsNoTrackingAsync(int? projectId, int? companyId)
        {
            try
            {
                Project? project = await _context.Projects
                    .Include(p => p.Company)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId && p.Archived == false);

                return project!;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }


}
