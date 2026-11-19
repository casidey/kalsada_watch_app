using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Converters;

namespace KalsadaWatchApp.Models.Enums;

/// <summary>Where a hazard report is in the repair workflow (matches the Verify map colors).</summary>
/// <remarks>Stored as text in Supabase. EnumMember gives the exact database value.</remarks>
[JsonConverter(typeof(PostgrestStringEnumConverter))]
public enum ReportStatus
{
    [EnumMember(Value = "submitted")] Submitted,
    [EnumMember(Value = "under_inspection")] UnderInspection,     // blue marker
    [EnumMember(Value = "under_construction")] UnderConstruction, // amber marker
    [EnumMember(Value = "repaired")] Repaired,                    // green marker
    [EnumMember(Value = "rejected")] Rejected
}
