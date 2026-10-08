namespace KalsadaWatchApp.Models;

/// <summary>An administrative district or operational area, e.g. "Central District".</summary>
public class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // Navigation
    public ICollection<User> Residents { get; set; } = new List<User>();
    public ICollection<HazardReport> HazardReports { get; set; } = new List<HazardReport>();
}
