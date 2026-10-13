using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EnrolDto
    {
        [Required]
        public int CategoryId { get; set; }
    }
}