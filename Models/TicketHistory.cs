using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BugTrackingSystem.Models
{
    public class TicketHistory
    {
        private DateTime _created;

        public int Id { get; set; }

        public int TicketId { get; set; }

        public string? PropertyName { get; set; }

        public string? Description { get; set; }

        public DateTime Created
        {
            get
            {
                return _created;
            }
            set
            {
                _created = value.ToUniversalTime();
            }
        }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [Required]
        public string? UserId { get; set; }

        // Navigation properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? User { get; set; }
    }
}
