using KalsadaWatchApp.Models.Enums;

namespace KalsadaWatchApp.Models;

/// <summary>One entry in a report's status timeline, e.g. "Crew dispatched".</summary>
public class StatusUpdate
{
    public int Id { get; set; }
    public int HazardReportId { get; set; }
    public ReportStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public int UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public HazardReport? HazardReport { get; set; }
    public User? UpdatedBy { get; set; }
}
