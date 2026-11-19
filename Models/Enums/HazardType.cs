using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Converters;

namespace KalsadaWatchApp.Models.Enums;

/// <summary>Kinds of road damage a citizen can report (matches the Report page options).</summary>
/// <remarks>Stored as text in Supabase. EnumMember gives the exact database value.</remarks>
[JsonConverter(typeof(PostgrestStringEnumConverter))]
public enum HazardType
{
    [EnumMember(Value = "deep_pothole")] DeepPothole,
    [EnumMember(Value = "alligator_cracks")] AlligatorCracks,
    [EnumMember(Value = "drain_collapse")] DrainCollapse,
    [EnumMember(Value = "shoulder_cavity")] ShoulderCavity
}
