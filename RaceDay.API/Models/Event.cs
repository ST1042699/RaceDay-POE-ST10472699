namespace RaceDay.API.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty; // 'Run', 'Walk', 'Cycle'[cite: 39]
    public decimal Distance { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}