using KalsadaWatchApp.Models.Enums;

namespace KalsadaWatchApp.Models;

/// <summary>A photo attached to a hazard report: evidence, repair progress, or completion proof.</summary>
public class ReportPhoto
{
    public int Id { get; set; }
    public int HazardReportId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public PhotoType Type { get; set; } = PhotoType.Evidence;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public HazardReport? HazardReport { get; set; }
}
