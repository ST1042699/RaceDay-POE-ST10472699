namespace RaceDay.MVC.Models;

public class EventViewModel
{
    public int EventID { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string EventType { get; set; } = "";
    public decimal Distance { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? BannerImageUrl { get; set; }
    public string Organiser { get; set; } = "";
    public List<CategoryViewModel> Categories { get; set; } = new();
}

public class CategoryViewModel
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKM { get; set; }
    public decimal EntryFee { get; set; }
}