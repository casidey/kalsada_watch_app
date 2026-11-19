using System.Globalization;

namespace KalsadaWatchApp.Components.Feedback;

/// <summary>One resident rating plus comment, as shown in the homepage feedback section.</summary>
public sealed record CitizenReview(
    string Id,
    string DisplayName,      // first name + last initial, or "Anonymous resident"
    string Area,             // barangay or street
    DateTimeOffset PostedAt,
    int? Rating,             // 1 to 5, or null for a comment-only review
    string? Comment,         // null for a rating-only review (at least one of the two is present)
    string? TicketId,        // optional linked report, e.g. "#KW-8910"
    bool IsResolved,         // true when the linked report is closed
    bool IsApproved);        // moderation flag: only approved reviews are shown

/// <summary>Where the feedback section gets its reviews. Swap the implementation to connect real data.</summary>
public interface IReviewSource
{
    Task<IReadOnlyList<CitizenReview>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Average and star breakdown, always calculated from the reviews passed in. Only reviews with a rating count.</summary>
public sealed record ReviewSummary(double Average, int Count, IReadOnlyList<ReviewSummary.Bucket> Distribution)
{
    public sealed record Bucket(int Stars, int Count, double Percent);

    public string AverageText => Average.ToString("0.0", CultureInfo.InvariantCulture);
    public int RoundedStars => (int)Math.Round(Average, MidpointRounding.AwayFromZero);

    public static ReviewSummary From(IReadOnlyList<CitizenReview> reviews)
    {
        var rated = reviews.Where(r => r.Rating is >= 1 and <= 5).Select(r => r.Rating!.Value).ToList();
        if (rated.Count == 0)
            return new ReviewSummary(0, 0, Array.Empty<Bucket>());

        var counts = Enumerable.Range(1, 5).Reverse()
            .Select(stars => (Stars: stars, Count: rated.Count(r => r == stars)))
            .ToList();
        var max = Math.Max(1, counts.Max(c => c.Count));
        var buckets = counts.Select(c => new Bucket(c.Stars, c.Count, 100.0 * c.Count / max)).ToList();
        return new ReviewSummary(rated.Average(), rated.Count, buckets);
    }
}
