using KalsadaWatchApp.Models.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>
/// A photo attached to a hazard report: evidence, repair progress, or completion proof. Table: report_photos.
/// </summary>
/// <remarks>The image file itself is in Supabase Storage (bucket "report-photos"); this row points to it.</remarks>
[Table("report_photos")]
public class ReportPhoto : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("hazard_report_id")]
    public long HazardReportId { get; set; }

    /// <summary>Where the file is inside the bucket, e.g. "KW-9002/3f2a….jpg".</summary>
    [Column("storage_path")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>The public link the pages use to show the photo.</summary>
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("photo_type")]
    public PhotoType Type { get; set; } = PhotoType.Evidence;

    [Column("uploaded_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime UploadedAt { get; set; }
}
