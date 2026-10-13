using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class UpdateEventDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public decimal Distance { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;
    }
}