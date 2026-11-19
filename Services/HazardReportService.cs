using System.Reflection;
using System.Runtime.Serialization;
using KalsadaWatchApp.Models;
using KalsadaWatchApp.Models.Enums;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;
using static Supabase.Postgrest.Constants;

namespace KalsadaWatchApp.Services;

/// <summary>How the report list is ordered by the database.</summary>
public enum ReportSort
{
    Newest,
    HighestPriority
}

/// <summary>
/// Every Supabase query for hazard reports and districts. Pages call these methods and get plain model lists back,
/// so the UI can change (e.g. to an Awesome Blazor component library) without touching database code.
/// </summary>
/// <remarks>Fast habits: filtering, searching and sorting happen in the database, and each method sends one request.</remarks>
public sealed class HazardReportService(Supabase.Client db)
{
    /// <summary>The statuses shown publicly on the map. Rejected reports are left out.</summary>
    public static readonly IReadOnlyList<ReportStatus> PublicStatuses = new[]
    {
        ReportStatus.Submitted, ReportStatus.UnderInspection, ReportStatus.UnderConstruction, ReportStatus.Repaired
    };

    public async Task<List<District>> GetDistrictsAsync()
    {
        var response = await db.From<District>()
            .Order("name", Ordering.Ascending)
            .Get();
        return response.Models;
    }

    /// <summary>The homepage numbers (report_dashboard view). Returns null if the view sent no row.</summary>
    public async Task<ReportDashboard?> GetDashboardAsync()
    {
        var response = await db.From<ReportDashboard>().Get();
        return response.Models.FirstOrDefault();
    }

    /// <summary>Active (not withdrawn) report counts per status, from the hazard_report_status_counts view.</summary>
    public async Task<List<ReportStatusCount>> GetStatusCountsAsync()
    {
        var response = await db.From<ReportStatusCount>().Get();
        return response.Models;
    }

    /// <summary>Active reports matching the filters. Withdrawn (soft-deleted) reports are always skipped.</summary>
    /// <param name="statuses">Only reports with one of these statuses.</param>
    /// <param name="districtId">Only reports in this district, or all districts when null.</param>
    /// <param name="search">Matches the landmark, ticket number, description or hazard type. Ignored when empty.</param>
    public async Task<List<HazardReport>> GetReportsAsync(
        IReadOnlyCollection<ReportStatus> statuses, long? districtId, string? search, ReportSort sort)
    {
        var query = db.From<HazardReport>()
            .Filter<string>("deleted_at", Operator.Is, null)
            .Filter("status", Operator.In, statuses.Select(status => ToDbValue(status)).ToList());

        if (districtId is long id)
            query = query.Filter("district_id", Operator.Equals, id);

        var term = CleanSearchTerm(search);
        if (term.Length > 0)
        {
            query = query.Or(new List<IPostgrestQueryFilter>
            {
                new QueryFilter("location_landmark", Operator.ILike, $"%{term}%"),
                new QueryFilter("ticket_number", Operator.ILike, $"%{term}%"),
                new QueryFilter("description", Operator.ILike, $"%{term}%"),
                // hazard_type values use underscores ("deep_pothole"); "_" also matches a space in ILIKE.
                new QueryFilter("hazard_type", Operator.ILike, $"%{term.Replace(' ', '_')}%"),
            });
        }

        query = sort == ReportSort.HighestPriority
            ? query.Order("severity_rank", Ordering.Descending).Order("submitted_at", Ordering.Descending)
            : query.Order("submitted_at", Ordering.Descending);

        var response = await query.Get();
        return response.Models;
    }

    /// <summary>A report's status timeline, oldest first (history table).</summary>
    public async Task<List<StatusUpdate>> GetHistoryAsync(long reportId)
    {
        var response = await db.From<StatusUpdate>()
            .Filter("hazard_report_id", Operator.Equals, reportId)
            .Order("updated_at", Ordering.Ascending)
            .Order("id", Ordering.Ascending)
            .Get();
        return response.Models;
    }

    /// <summary>Saves a new report. The database assigns the id, ticket number and dates.</summary>
    /// <returns>The saved report, including its new ticket number.</returns>
    public async Task<HazardReport> CreateAsync(HazardReport report)
    {
        report.Status = ReportStatus.Submitted;
        report.StatusNote = "Report received from citizen.";

        var response = await db.From<HazardReport>().Insert(report);
        return response.Model ?? throw new InvalidOperationException("Supabase did not return the new report.");
    }

    /// <summary>The Supabase Storage bucket that holds report photos.</summary>
    public const string PhotoBucket = "report-photos";

    /// <summary>
    /// Uploads photos for a saved report: each file goes to Storage, then one request records them all in report_photos.
    /// </summary>
    /// <param name="jpegPhotos">JPEG files (the Report page converts and shrinks them in the browser first).</param>
    public async Task<List<ReportPhoto>> AddPhotosAsync(HazardReport report, IReadOnlyList<byte[]> jpegPhotos)
    {
        if (jpegPhotos.Count == 0)
            return new List<ReportPhoto>();

        var bucket = db.Storage.From(PhotoBucket);
        var rows = new List<ReportPhoto>();

        foreach (var jpeg in jpegPhotos)
        {
            var path = $"{report.TicketNumber}/{Guid.NewGuid():N}.jpg";
            await bucket.Upload(jpeg, path, new Supabase.Storage.FileOptions { ContentType = "image/jpeg", Upsert = false });

            rows.Add(new ReportPhoto
            {
                HazardReportId = report.Id,
                StoragePath = path,
                ImageUrl = bucket.GetPublicUrl(path),
                Type = PhotoType.Evidence
            });
        }

        var response = await db.From<ReportPhoto>().Insert(rows);
        return response.Models;
    }

    /// <summary>A report's photos, oldest first.</summary>
    public async Task<List<ReportPhoto>> GetPhotosAsync(long reportId)
    {
        var response = await db.From<ReportPhoto>()
            .Filter("hazard_report_id", Operator.Equals, reportId)
            .Order("uploaded_at", Ordering.Ascending)
            .Get();
        return response.Models;
    }

    /// <summary>Moves a report to a new status. The database records the change in status_updates.</summary>
    public async Task ChangeStatusAsync(long reportId, ReportStatus status, string note)
    {
        await db.From<HazardReport>()
            .Filter("id", Operator.Equals, reportId)
            .Set(x => x.Status, status)
            .Set(x => x.StatusNote, note)
            .Update();
    }

    /// <summary>Soft delete: hides the report everywhere but keeps the row and its history.</summary>
    public async Task WithdrawAsync(long reportId)
    {
        await db.From<HazardReport>()
            .Filter("id", Operator.Equals, reportId)
            .Filter<string>("deleted_at", Operator.Is, null)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Update();
    }

    /// <summary>The text stored in the database for an enum value, e.g. UnderInspection -> "under_inspection".</summary>
    public static string ToDbValue<TEnum>(TEnum value) where TEnum : struct, Enum
        => typeof(TEnum).GetField(value.ToString())?.GetCustomAttribute<EnumMemberAttribute>()?.Value
           ?? value.ToString();

    /// <summary>Removes characters that have a special meaning in a PostgREST filter, and caps the length.</summary>
    private static string CleanSearchTerm(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return string.Empty;

        var kept = search.Where(c => c is not (',' or '(' or ')' or '*' or '%' or '"' or '\\' or ':')).ToArray();
        var term = new string(kept).Trim();
        return term.Length > 60 ? term[..60] : term;
    }
}
