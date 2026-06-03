using System.ComponentModel.DataAnnotations;

namespace DocumentManagementApp.Models
{
    public class ApplicationUser
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}