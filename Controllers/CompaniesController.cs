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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles()
        {
            // 1 - Add an instance of the ViewModel as a List (model)
            List<ManageUserRolesViewModel> model = new List<ManageUserRolesViewModel>();

            // 2 - Get CompanyId 
            //

            // 3 - Get all company users
            List<BTUser> members = await _companyService.GetMembersAsync(_companyId);

            // 4 - Loop over the users to populate the ViewModel
            //      - instantiate single ViewModel
            //      - use _rolesService
            //      - Create multiselect
            //      - viewmodel to model
            string? btUserId = _userManager.GetUserId(User);

            foreach (BTUser member in members)
            {
                if (string.Compare(btUserId, member.Id) != 0)
                {
                    ManageUserRolesViewModel viewModel = new();

                    IEnumerable<string>? currentRoles = await _rolesService.GetUserRolesAsync(member);

                    viewModel.BTUser = member;
                    viewModel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", currentRoles);
                    // Set the CurrentRole property
                    viewModel.CurrentRole = currentRoles?.FirstOrDefault();

                    model.Add(viewModel);
                }
            }


            // 5 - Return the model to the View
            return View(model);
        }



        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel viewModel)
        {


            
            // 1- Get the company Id
            //

            // 2 - Instantiate the BTUser
            BTUser? btUser = (await _companyService.GetMembersAsync(_companyId)).FirstOrDefault(m => m.Id == viewModel.BTUser?.Id);

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
