using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}