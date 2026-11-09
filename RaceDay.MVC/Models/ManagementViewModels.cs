using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    [Range(0.01, 999.99)]
    public decimal Distance { get; set; }

    [Required]
    public DateTime EventDate { get; set; } =
        DateTime.Today.AddDays(7);

    [Required]
    public string Location { get; set; } = "";
}

public class CategoryFormViewModel
{
    [Required]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99)]
    public decimal DistanceKM { get; set; }

    [Range(0, 999999)]
    public decimal EntryFee { get; set; }
}

public class EnrolFormViewModel
{
    [Required]
    public int CategoryId { get; set; }
}

public class ResultFormViewModel
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required]
    [RegularExpression(
        @"^\d{1,2}:\d{2}:\d{2}$",
        ErrorMessage = "Enter the time as HH:mm:ss.")]
    public string CompletionTime { get; set; } = "";

    [Range(1, 100000)]
    public int RankPosition { get; set; }
}

public class EnrolmentViewModel
{
    public int EnrolmentID { get; set; }
    public string Participant { get; set; } = "";
    public string Email { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime EnrolledAt { get; set; }
}

public class MyEnrolmentViewModel
{
    public int EnrolmentID { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string Category { get; set; } = "";
    public string Event { get; set; } = "";
    public DateTime EventDate { get; set; }
}

public class ResultViewModel
{
    public int ResultID { get; set; }
    public string Event { get; set; } = "";
    public string Category { get; set; } = "";
    public string CompletionTime { get; set; } = "";
    public int RankPosition { get; set; }
    public DateTime RecordedAt { get; set; }
}