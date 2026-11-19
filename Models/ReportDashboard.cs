using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>Live numbers for the homepage, calculated by the database. View: report_dashboard (always one row).</summary>
[Table("report_dashboard")]
public class ReportDashboard : BaseModel
{
    /// <summary>All reports that have not been withdrawn.</summary>
    [Column("total_reports")]
    public int TotalReports { get; set; }

    [Column("repaired_reports")]
    public int RepairedReports { get; set; }

    /// <summary>Submitted or under inspection.</summary>
    [Column("inspection_reports")]
    public int InspectionReports { get; set; }

    [Column("construction_reports")]
    public int ConstructionReports { get; set; }

    [Column("district_count")]
    public int DistrictCount { get; set; }

    /// <summary>Average hours from submission to the first status change. Empty when no report has been acted on yet.</summary>
    [Column("avg_response_hours")]
    public double? AvgResponseHours { get; set; }

    /// <summary>When any report's status last changed.</summary>
    [Column("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }

    /// <summary>The district with the most open (not yet repaired) reports.</summary>
    [Column("busiest_district")]
    public string? BusiestDistrict { get; set; }

    [Column("busiest_district_open")]
    public int BusiestDistrictOpen { get; set; }
}
