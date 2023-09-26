using System.ComponentModel.DataAnnotations;

namespace BugTrackingSystem.Models
{
    public class TicketStatus
    {      
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}