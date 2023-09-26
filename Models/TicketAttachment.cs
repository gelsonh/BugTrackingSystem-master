using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using BugTrackingSystem.Extensions;
using System.ComponentModel;

namespace BugTrackingSystem.Models
{
    public class TicketAttachment
    {
        private DateTime _created;

        public int Id { get; set; }

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

        public int TicketId { get; set; }

        [Required]
        public string? BTUserId { get; set; }

        [NotMapped]
        [DisplayName("Select a file")]
        [DataType(DataType.Upload)]
        [MaxFileSize(1024 * 1024)]
        [AllowedExtensions(new string[] { ".jpg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".pdf" })]
        public IFormFile? FormFile { get; set; }

        public byte[]? FileData { get; set; }

        public string? FileType { get; set; }

        // Navigation properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? BTUser { get; set; }
        public string? FileName { get; internal set; }
        public string? FileContentType { get; internal set; }
    }
}
