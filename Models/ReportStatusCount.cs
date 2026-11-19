using KalsadaWatchApp.Models.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>How many active reports have a given status. View: hazard_report_status_counts (read-only).</summary>
[Table("hazard_report_status_counts")]
public class ReportStatusCount : BaseModel
{
    [Column("status")]
    public ReportStatus Status { get; set; }

    [Column("total")]
    public int Total { get; set; }
}
