using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KalsadaWatchApp.Models;

/// <summary>An administrative district or operational area, e.g. "Central District". Table: districts.</summary>
[Table("districts")]
public class District : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("city")]
    public string City { get; set; } = string.Empty;
}
