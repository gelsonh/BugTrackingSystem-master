using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTrackingSystem.Models.ViewModels
{
    public class AssignPMViewModel
    {
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public SelectList? PMList { get; set; }
        public string? PMId { get; set; }
    }
}
