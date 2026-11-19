using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace KalsadaWatchApp.Components.Feedback;

/// <summary>Real reviews from Supabase: approved, not removed, newest first. One request per page load.</summary>
public sealed class SupabaseReviewSource(Supabase.Client db) : IReviewSource
{
    public async Task<IReadOnlyList<CitizenReview>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default)
    {
        var response = await db.From<ApprovedReviewRow>()
            .Order("posted_at", Ordering.Descending)
            .Get(cancellationToken);

        return response.Models
            .Select(row => new CitizenReview(
                Id: row.Id.ToString(),
                DisplayName: row.DisplayName,
                Area: row.Area,
                PostedAt: new DateTimeOffset(row.PostedAt.ToUniversalTime()),
                Rating: row.Rating,
                Comment: row.Comment,
                TicketId: row.TicketNumber is null ? null : $"#{row.TicketNumber}",
                IsResolved: row.IsResolved,
                IsApproved: true))
            .ToList();
    }

    /// <summary>One row of the approved_reviews view (read-only).</summary>
    [Table("approved_reviews")]
    public sealed class ApprovedReviewRow : BaseModel
    {
        [Column("id")] public long Id { get; set; }
        [Column("display_name")] public string DisplayName { get; set; } = string.Empty;
        [Column("area")] public string Area { get; set; } = string.Empty;
        [Column("posted_at")] public DateTime PostedAt { get; set; }
        [Column("rating")] public int? Rating { get; set; }
        [Column("comment")] public string? Comment { get; set; }
        [Column("ticket_number")] public string? TicketNumber { get; set; }
        [Column("is_resolved")] public bool IsResolved { get; set; }
    }
}
