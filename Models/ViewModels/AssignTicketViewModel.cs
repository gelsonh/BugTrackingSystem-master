using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTrackingSystem.Models.ViewModels
{
    public class AssignTicketViewModel
    {
        public Ticket? Ticket { get; set; }
        public SelectList? Developers { get; set; }
        public string? DeveloperId { get; set; }
    }
}
