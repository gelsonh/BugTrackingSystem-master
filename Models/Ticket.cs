using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BugTrackingSystem.Models
{
    public class Ticket
    {
        private DateTime _created;
        private DateTime? _updated;
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        [Required]
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

        public DateTime? Updated
        {
            get => _updated;

            set
            {
                if (value.HasValue)
                {
                    _updated = value.Value.ToUniversalTime();
                }
                else
                {
                    _updated = null;
                }
            }
        }

        public bool Archived { get; set; }

        public bool ArchivedByProject { get; set; }

        [Required]
        public int ProjectId { get; set; }


        public int TicketTypeId { get; set; }

        public int TicketStatusId { get; set; }


        public int TicketPriorityId { get; set; }

        public string? DeveloperUserId { get; set; }

        [Required]
        public string? SubmitterUserId { get; set; }

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual TicketPriority? TicketPriority { get; set; }
        public virtual TicketType? TicketType { get; set; }
        public virtual TicketStatus? TicketStatus { get; set; }
        public virtual BTUser? DeveloperUser { get; set; }
        public virtual BTUser? SubmitterUser { get; set; }

        // Collections

        public virtual ICollection<TicketHistory> History { get; set; } = new HashSet<TicketHistory>();
        public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();


    }
}