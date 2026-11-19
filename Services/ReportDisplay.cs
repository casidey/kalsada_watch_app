using KalsadaWatchApp.Models;
using KalsadaWatchApp.Models.Enums;

namespace KalsadaWatchApp.Services;

/// <summary>One pin on a Leaflet map (wwwroot/js/kalsada-map.js reads these property names).</summary>
public sealed record MapPin(
    string Id,              // ticket number, e.g. "KW-8921"
    string Title,
    string Location,
    string District,
    string Status,          // "Under Construction", "Finished" or "Under Inspection"
    string Severity,        // "High", "Medium" or "Low"
    int? ProgressPercent,   // only known for finished repairs
    double? Lat,
    double? Lng);

/// <summary>Turns database values into the words and formats the pages show, so every page says the same thing.</summary>
public static class ReportDisplay
{
    public static string HazardLabel(HazardType type) => type switch
    {
        HazardType.DeepPothole => "Deep Pothole",
        HazardType.AlligatorCracks => "Alligator / Surface Cracks",
        HazardType.DrainCollapse => "Drain / Manhole Collapse",
        HazardType.ShoulderCavity => "Road Shoulder Cavity",
        _ => type.ToString()
    };

    /// <summary>The full status name, used in the status history.</summary>
    public static string StatusLabel(ReportStatus status) => status switch
    {
        ReportStatus.Submitted => "Submitted",
        ReportStatus.UnderInspection => "Under Inspection",
        ReportStatus.UnderConstruction => "Under Construction",
        ReportStatus.Repaired => "Repaired",
        ReportStatus.Rejected => "Rejected",
        _ => status.ToString()
    };

    /// <summary>The three public map groups: amber, green and blue pins.</summary>
    public static string MapStatus(ReportStatus status) => status switch
    {
        ReportStatus.UnderConstruction => "Under Construction",
        ReportStatus.Repaired => "Finished",
        _ => "Under Inspection"
    };

    public static MapPin ToMapPin(HazardReport report, string districtName) => new(
        Id: report.TicketNumber,
        Title: HazardLabel(report.HazardType),
        Location: report.LocationLandmark,
        District: districtName,
        Status: MapStatus(report.Status),
        Severity: report.Severity.ToString(),
        ProgressPercent: report.Status == ReportStatus.Repaired ? 100 : null,
        Lat: report.Latitude,
        Lng: report.Longitude);

    public static string FormatLocal(DateTime value) => value.ToLocalTime().ToString("MMM d, h:mm tt");

    public static string TimeAgo(DateTime value)
    {
        var age = DateTime.UtcNow - value.ToUniversalTime();
        if (age < TimeSpan.FromMinutes(1)) return "Just now";
        if (age < TimeSpan.FromHours(1)) return Plural((int)age.TotalMinutes, "min") + " ago";
        if (age < TimeSpan.FromDays(1)) return Plural((int)age.TotalHours, "hour") + " ago";
        if (age < TimeSpan.FromDays(2)) return "Yesterday";
        if (age < TimeSpan.FromDays(7)) return Plural((int)age.TotalDays, "day") + " ago";
        return FormatLocal(value);
    }

    public static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
