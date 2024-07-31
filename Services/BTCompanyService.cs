using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BugTrackingSystem.Services
{
    public class BTCompanyService : IBTCompanyService
    {

        private readonly ApplicationDbContext _context;
        public BTCompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        // This method retrieves a company's information based on its ID.
        public async Task<Company?> GetCompanyInfoAsync(int? companyId)
        {
            // Check if the companyId is null.
            // If it is, return null as we can't find a company without an ID.
            if (companyId == null)
            {
                return null;
            }

            try
            {
                // Attempt to find a company with the provided ID in the database.
                // The FindAsync method returns the first entry it finds with the provided ID.
                Company? company = await _context.Companies.FindAsync(companyId);

                // If no company was found with the provided ID, return null.
                if (company == null)
                {
                    return null;
                }

                // If a company was found, return it.
                return company;
            }
            catch (Exception)
            {
                throw;
               
            }
        }


        public Task<List<Invite>> GetInvitesAsync(int? companyId)
        {
            throw new NotImplementedException();
        }


        public async Task<List<BTUser>> GetMembersAsync(int? companyId)
        {
            try
            {
                List<BTUser> members = new List<BTUser>();

                members = await _context.Users.Where(u => u.CompanyId == companyId).ToListAsync();

                return members;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<List<Project>> GetProjectsAsync(int? companyId)

        {
            if (companyId == null)
            {
                return new List<Project>();
            }

            List<Project> projects = await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();

            return projects;
        }
       
       
        public async Task<int?> GetCompanyIdByUserIdAsync(string userId)
        {
            BTUser? user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return null;
            }

            return user.CompanyId;
        }


    }
}
