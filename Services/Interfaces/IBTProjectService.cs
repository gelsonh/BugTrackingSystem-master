using BugTrackingSystem.Data;
using BugTrackingSystem.Models;


namespace BugTrackingSystem.Services.Interfaces
{
    public interface IBTProjectService
    {

        public Task AssignDefautProjectManagersAsync(ApplicationDbContext context);
        public Task AddProjectAsync(Project project);
        public Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId);
        public Task AddMembersToProjectAsync(IEnumerable<string>? userIds, int? projectId, int? companyId);

        public Task<bool> AddProjectManagerAsync(string? userId, int? projectId);
        public Task ArchiveProjectAsync(Project? project, int? companyId);
        public Task<List<Project>> GetAllProjectsByCompanyIdAsync(int? companyId);
        public Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int? companyId);
        public Task<Project> GetProjectByIdAsync(int? projectId, int? companyId);
       
        public Task<BTUser> GetProjectManagerAsync(int? projectId);
        public Task<List<BTUser>> GetProjectMembersByRoleAsync(int? projectId, string? roleName, int? companyId);
        public Task<IEnumerable<ProjectPriority>> GetProjectPrioritiesAsync();
        public Task<List<Project>?> GetUserProjectsAsync(string? userId);

        public Task<List<Project>> GetUnassignedProjectsAsync(int? companyId);

        public Task RemoveMembersFromProjectAsync(int? projectId, int? companyId);

        public Task RemoveProjectManagerAsync(int? projectId);
        public Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId);
        public Task RestoreProjectAsync(Project? project, int? companyId);
        public Task UpdateProjectAsync(Project? project);
        Task<Project> GetProjectAsNoTrackingAsync(int? id, int? companyId);
    }
}
