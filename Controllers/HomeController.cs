using BugTrackingSystem.Models;
using BugTrackingSystem.Models.ViewModels;
using BugTrackingSystem.Services;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BugTrackingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTCompanyService _companyService;
        private readonly IBTProjectService _projectService;
        private readonly IBTTicketService _ticketService;

        public HomeController(ILogger<HomeController> logger,UserManager<BTUser> userManager, IBTCompanyService bTCompanyService, IBTProjectService projectService, IBTTicketService ticketService)
        {
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

        




    }
}
