using KalsadaWatchApp.Models.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>
/// A road hazard lodged by a citizen. Each report is one repair ticket, e.g. "KW-8921". Table: hazard_reports.
/// </summary>
/// <remarks>
/// Columns marked ignoreOnInsert are filled in by the database (defaults and triggers), so the app never sends them.
/// Changing <see cref="Status"/> makes the database add a row to status_updates (the report's history).
/// </remarks>
[Table("hazard_reports")]
public class HazardReport : BaseModel
{
    /// <summary>The promised time for a field inspection after a report is submitted (the database uses the same 48 hours).</summary>
    public static readonly TimeSpan InspectionSla = TimeSpan.FromHours(48);

    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("ticket_number", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public string TicketNumber { get; set; } = string.Empty;

    [Column("district_id")]
    public long DistrictId { get; set; }

    [Column("reporter_id")]
    public long? ReporterId { get; set; }

    [Column("location_landmark")]
    public string LocationLandmark { get; set; } = string.Empty;

    /// <summary>Empty when the citizen did not pin an exact spot. Such reports are listed but not drawn on the map.</summary>
    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("hazard_type")]
    public HazardType HazardType { get; set; }

    [Column("severity")]
    public SeverityLevel Severity { get; set; }

    [Column("status")]
    public ReportStatus Status { get; set; } = ReportStatus.Submitted;

    /// <summary>The note that goes with the latest status change. It is copied into the history row.</summary>
    [Column("status_note")]
    public string? StatusNote { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("submitted_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime SubmittedAt { get; set; }

    [Column("inspection_due_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime InspectionDueAt { get; set; }

    /// <summary>Set by the database when the status becomes Repaired.</summary>
    [Column("resolved_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Soft delete: a withdrawn report keeps its row and gets this date.</summary>
    [Column("deleted_at", ignoreOnInsert: true)]
    public DateTime? DeletedAt { get; set; }

    /// <summary>True when the report is still waiting for inspection past the 48-hour deadline.</summary>
    public bool IsSlaBreached()
        => Status == ReportStatus.Submitted && DateTime.UtcNow > InspectionDueAt.ToUniversalTime();
}
