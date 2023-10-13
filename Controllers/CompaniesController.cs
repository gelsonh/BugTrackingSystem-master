using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTrackingSystem.Data;
using BugTrackingSystem.Models;
using BugTrackingSystem.Services;
using BugTrackingSystem.Services.Interfaces;
using BugTrackingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Azure;
using NuGet.Protocol.Plugins;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using System.Runtime.Intrinsics.X86;

namespace BugTrackingSystem.Controllers
{
    [Authorize]
    public class CompaniesController : BTBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTCompanyService _companyService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRolesService _rolesService;

        public CompaniesController(ApplicationDbContext context, IBTCompanyService companyService, UserManager<BTUser> userManager, IBTRolesService rolesService)
        {
            _context = context;
            _companyService = companyService;
            _userManager = userManager;
            _rolesService = rolesService;
        }

        //// GET: Companies
        //public async Task<IActionResult> Index()
        //{
        //      return _context.Companies != null ? 
        //                  View(await _context.Companies.ToListAsync()) :
        //                  Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
        //}

        [Authorize(Roles = "Admin, DemoUser")]
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles()
        {
            // 1 - Create an empty list to hold the ViewModel instances
            List<ManageUserRolesViewModel> model = new List<ManageUserRolesViewModel>();

            // 2 - Get the CompanyId (replace this with your method to obtain the company ID)
            int? companyId = (await _userManager.GetUserAsync(User))?.CompanyId;

            // 3 - Get all company users
            if (companyId.HasValue)
            {
                List<BTUser> members = await _companyService.GetMembersAsync(companyId.Value);

                // 4 - Loop over the users to populate the ViewModel
                foreach (BTUser member in members)
                {
                    // Instantiate a single ViewModel
                    ManageUserRolesViewModel viewModel = new ManageUserRolesViewModel();

                    // Use _rolesService to get current roles for the user
                    IEnumerable<string>? currentRoles = await _rolesService.GetUserRolesAsync(member);

                    // Assign properties to the ViewModel
                    viewModel.BTUser = member;
                    viewModel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", currentRoles);

                    // Get the current role for the user (if you want to set the user's current role)
                    string currentRole = await _rolesService.GetCurrentRoleAsync(member);

                    // Set the CurrentRole property for this user
                    viewModel.CurrentRole = currentRole;

                    // Add the ViewModel to the list
                    model.Add(viewModel);
                }
            }
            else
            {
                // Handle the case where companyId is null (you can log, redirect, or display an error)
                // For example, you can log an error and redirect the user to an error page.
                // Logging.LogError("CompanyId is null for the current user.");
                return RedirectToAction("ErrorPage"); // Replace with your error handling logic.
            }

            // 5 - Return the model to the View
            return View(model);
        }




        [Authorize(Roles = "Admin, DemoUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel viewModel)
        {
            // 1- Get the company Id
            int? companyId = (await _userManager.GetUserAsync(User))?.CompanyId;

            // 2 - Instantiate the BTUser
            BTUser? btUser = (await _companyService.GetMembersAsync(companyId)).FirstOrDefault(m => m.Id == viewModel.BTUser?.Id);

            // 3 - Get Roles for the User
            IEnumerable<string>? currentRoles = await _rolesService.GetUserRolesAsync(btUser);

            // 4 - Get Selected Role(s) for the User
            IEnumerable<string>? selectedRoles = viewModel.SelectedRoles;

            // 5 - Remove current role(s) and Add new role(s)
            if (selectedRoles != null && selectedRoles.Any())
            {
                if (await _rolesService.RemoveUserFromRolesAsync(btUser, currentRoles))
                {
                    foreach (var role in selectedRoles)
                    {
                        await _rolesService.AddUserToRoleAsync(btUser, role);
                    }
                }
            }

            // 6 - Navigate
            return RedirectToAction(nameof(ManageUserRoles));
        }





        // GET: Companies/Details/5
        public async Task<IActionResult> Details()
        {

            var company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == _companyId);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // GET: Companies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ImageFileData,ImageFileType")] Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Companies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Companies == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: Companies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ImageFileData,ImageFileType")] Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    //if (!CompanyExists(company.Id))
                    //{
                    //    return NotFound();
                    //}
                    //else
                    //{
                    //    throw;
                    //}
                }
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        //// GET: Companies/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null || _context.Companies == null)
        //    {
        //        return NotFound();
        //    }

        //    var company = await _context.Companies
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (company == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(company);
        //}

        //// POST: Companies/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    if (_context.Companies == null)
        //    {
        //        return Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
        //    }
        //    var company = await _context.Companies.FindAsync(id);
        //    if (company != null)
        //    {
        //        _context.Companies.Remove(company);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool CompanyExists(int id)
        //{
        //  return (_context.Companies?.Any(e => e.Id == id)).GetValueOrDefault();
        //}
    }
}
