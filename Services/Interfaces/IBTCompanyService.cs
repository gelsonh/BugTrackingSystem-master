using BugTrackingSystem.Models;

namespace BugTrackingSystem.Services.Interfaces
{
    public interface IBTCompanyService
    {
        public Task<Company?> GetCompanyInfoAsync(int? companyId);
    
   
        public Task<List<BTUser>> GetMembersAsync(int? companyId);

        public Task<List<Project>> GetProjectsAsync(int? companyId);

        public Task<List<Invite>> GetInvitesAsync(int? companyId);
        public Task<int?> GetCompanyIdByUserIdAsync(string userId);
    }
}
