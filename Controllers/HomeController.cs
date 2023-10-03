using BugTrackingSystem.Data;
using BugTrackingSystem.Extensions;
using BugTrackingSystem.Models;
using BugTrackingSystem.Models.Enums;
using BugTrackingSystem.Models.ViewModels;
using BugTrackingSystem.Services;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BugTrackingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTCompanyService _companyService;
        private readonly IBTProjectService _projectService;
        private readonly IBTTicketService _ticketService;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger,UserManager<BTUser> userManager, IBTCompanyService bTCompanyService, IBTProjectService projectService, IBTTicketService ticketService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _companyService = bTCompanyService;
            _projectService = projectService;
            _ticketService = ticketService;

        }

        public IActionResult Index()
        {
            
            return View();
          
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel();

            var currentUser = await _userManager.GetUserAsync(User);

            var companyId = await _companyService.GetCompanyIdByUserIdAsync(currentUser!.Id);

            model.Company = await _companyService.GetCompanyInfoAsync(companyId);
            model.Projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);
            model.Tickets = await _ticketService.GetAllTicketsByCompanyIdAsync(companyId);
            model.Members = await _companyService.GetMembersAsync(companyId);

            return View(model);
        }


        [HttpPost]
        public async Task<JsonResult> PlotlyBarChart()
        {
            PlotlyBarData plotlyData = new();
            List<PlotlyBar> barData = new();

            int? companyId = User.Identity?.GetCompanyId();
            //int? companyId = User.Identity?.GetCompanyId().Value;
           
            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);

            //Bar One
            PlotlyBar barOne = new()
            {
                X = projects.Select(p => p.Name).ToArray()!,
                Y = projects.SelectMany(p => p.Tickets).GroupBy(t => t.ProjectId).Select(g => g.Count()).ToArray(),
                Name = "Tickets",
                Type = "bar"
            };

            //Bar Two
            PlotlyBar barTwo = new()
            {
                X = projects.Select(p => p.Name).ToArray()!,
                Y = projects.Select(async p => (await _projectService.GetProjectMembersByRoleAsync(p.Id, nameof(BTRoles.Developer), companyId)).Count).Select(c => c.Result).ToArray(),
                Name = "Developers",
                Type = "bar"
            };

            barData.Add(barOne);
            barData.Add(barTwo);

            plotlyData.Data = barData;

            return Json(plotlyData);
        }

      


    }
}
