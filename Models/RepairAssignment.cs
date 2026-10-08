namespace KalsadaWatchApp.Models;

/// <summary>Links a hazard report to the crew fixing it, and tracks repair progress.</summary>
public class RepairAssignment
{
    public int Id { get; set; }
    public int HazardReportId { get; set; }
    public int RepairCrewId { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedCompletion { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public HazardReport? HazardReport { get; set; }
    public RepairCrew? RepairCrew { get; set; }

    /// <summary>Sets progress (0–100). Reaching 100 marks the repair as completed.</summary>
    public void UpdateProgress(int percent)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
        CompletedAt = ProgressPercent == 100 ? DateTime.UtcNow : null;
    }
}
