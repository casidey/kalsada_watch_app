using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Converters;

namespace KalsadaWatchApp.Models.Enums;

/// <summary>What a photo attached to a report shows.</summary>
/// <remarks>Stored as text in Supabase. EnumMember gives the exact database value.</remarks>
[JsonConverter(typeof(PostgrestStringEnumConverter))]
public enum PhotoType
{
    [EnumMember(Value = "evidence")] Evidence,     // taken by the citizen when reporting
    [EnumMember(Value = "progress")] Progress,     // taken by the crew during repair
    [EnumMember(Value = "completion")] Completion  // proof that the road was fixed
}
