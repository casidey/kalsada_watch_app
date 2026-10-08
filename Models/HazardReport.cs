using KalsadaWatchApp.Models.Enums;

namespace KalsadaWatchApp.Models;

/// <summary>A road hazard lodged by a citizen. Each report is one repair ticket, e.g. "KW-8921".</summary>
public class HazardReport
{
    /// <summary>The promised time for a field inspection after a report is submitted.</summary>
    public static readonly TimeSpan InspectionSla = TimeSpan.FromHours(48);

    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int ReporterId { get; set; }
    public int DistrictId { get; set; }
    public string LocationLandmark { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public HazardType HazardType { get; set; }
    public SeverityLevel Severity { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Submitted;
    public string Description { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime InspectionDueAt { get; set; } = DateTime.UtcNow.Add(InspectionSla);
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public User? Reporter { get; set; }
    public District? District { get; set; }
    public RepairAssignment? RepairAssignment { get; set; }
    public ICollection<ReportPhoto> Photos { get; set; } = new List<ReportPhoto>();
    public ICollection<StatusUpdate> StatusHistory { get; set; } = new List<StatusUpdate>();
    public ICollection<CitizenVerification> Verifications { get; set; } = new List<CitizenVerification>();

    /// <summary>Changes the status and records the change in the report's history.</summary>
    public void UpdateStatus(ReportStatus status, string note, int updatedById)
    {
        Status = status;
        ResolvedAt = status == ReportStatus.Repaired ? DateTime.UtcNow : null;

        StatusHistory.Add(new StatusUpdate
        {
            HazardReportId = Id,
            Status = status,
            Note = note,
            UpdatedById = updatedById,
            UpdatedAt = DateTime.UtcNow
        });
    }

    /// <summary>True when the report is still waiting for inspection past the 48-hour deadline.</summary>
    public bool IsSlaBreached()
        => Status == ReportStatus.Submitted && DateTime.UtcNow > InspectionDueAt;
}
