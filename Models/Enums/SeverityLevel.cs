using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Converters;

namespace KalsadaWatchApp.Models.Enums;

/// <summary>How urgent a reported hazard is.</summary>
/// <remarks>Stored as text in Supabase. EnumMember gives the exact database value.</remarks>
[JsonConverter(typeof(PostgrestStringEnumConverter))]
public enum SeverityLevel
{
    [EnumMember(Value = "low")] Low,
    [EnumMember(Value = "medium")] Medium,
    [EnumMember(Value = "high")] High
}
