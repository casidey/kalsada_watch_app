namespace KalsadaWatchApp.Models;

/// <summary>A municipal repair team, e.g. "Arterial Road Division Unit 01".</summary>
public class RepairCrew
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;

    // Navigation
    public ICollection<RepairAssignment> Assignments { get; set; } = new List<RepairAssignment>();
}
