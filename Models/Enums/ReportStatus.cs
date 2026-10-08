namespace KalsadaWatchApp.Models.Enums;

/// <summary>Where a hazard report is in the repair workflow (matches the Verify map colors).</summary>
public enum ReportStatus
{
    Submitted,
    UnderInspection,   // blue marker
    UnderConstruction, // amber marker
    Repaired,          // green marker
    Rejected
}
