using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateResultDto
    {
        [Required]
        public int EnrolmentId { get; set; }

        [Required]
        public string CompletionTime { get; set; } = string.Empty;

        [Required]
        public int RankPosition { get; set; }
    }
}