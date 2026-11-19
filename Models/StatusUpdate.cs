using KalsadaWatchApp.Models.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>
/// One entry in a report's status timeline, e.g. "Crew dispatched". Table: status_updates.
/// </summary>
/// <remarks>History table: the database writes these rows itself when a report's status changes. The app only reads them.</remarks>
[Table("status_updates")]
public class StatusUpdate : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("hazard_report_id")]
    public long HazardReportId { get; set; }

    [Column("status")]
    public ReportStatus Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("updated_by_id")]
    public long? UpdatedById { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
