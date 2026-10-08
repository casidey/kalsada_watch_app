namespace KalsadaWatchApp.Models;

/// <summary>A citizen confirming (or disputing) that a reported road was actually repaired.</summary>
public class CitizenVerification
{
    public int Id { get; set; }
    public int HazardReportId { get; set; }
    public int UserId { get; set; }
    public bool ConfirmsRepair { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public HazardReport? HazardReport { get; set; }
    public User? User { get; set; }
}
