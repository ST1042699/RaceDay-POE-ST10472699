using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        public decimal DistanceKM { get; set; }

        [Required]
        public decimal EntryFee { get; set; }
    }
}