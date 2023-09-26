using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BugTrackingSystem.Models
{
    public class Invite
    {
        private DateTime _inviteDate;
        private DateTime? _joinDate;
        public int Id { get; set; }

        public DateTime InviteDate
        {
            get
            {
                return _inviteDate;
            }
            set
            {
                _inviteDate = value.ToUniversalTime();
            }
        }

        public DateTime? JoinDate
        {
            get
            {
                return _joinDate;
            }
            set
            {
                if (value.HasValue)
                {
                    _joinDate = value.Value.ToUniversalTime();
                }
                else
                {
                    _joinDate = null;
                }
            }
        }

        public Guid CompanyToken { get; set; }
      
        public int CompanyId { get; set; }
    
        public int? ProjectId { get; set; }

        [Required]
        public string? InvitorId { get; set; }

        public string? InviteeId { get; set; }

        [Required]
        public string? InviteeEmail { get; set; }

        [Required]
        public string? InviteeFirstName { get; set; }

        [Required]
        public string? InviteeLastName { get; set; }

        public string? Message { get; set; }
        public bool IsValid { get; set; }

        // Navigation properties
        public virtual Company? Company { get; set; } 
        public virtual Project? Project { get; set; } 
        public virtual BTUser? Invitor { get; set; }
        public virtual BTUser? Invitee { get; set; }
    }

}
