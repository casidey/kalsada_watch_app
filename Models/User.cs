using KalsadaWatchApp.Models.Enums;

namespace KalsadaWatchApp.Models;

/// <summary>A registered account: a citizen, a municipal engineer, or an admin.</summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // never store the plain password
    public string PhoneNumber { get; set; } = string.Empty;
    public string GovernmentIdNumber { get; set; } = string.Empty; // e.g. PhilSys number
    public UserRole Role { get; set; } = UserRole.Citizen;
    public int DistrictId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public District? District { get; set; }
    public ICollection<HazardReport> SubmittedReports { get; set; } = new List<HazardReport>();
    public ICollection<CitizenVerification> Verifications { get; set; } = new List<CitizenVerification>();
    public ICollection<StatusUpdate> StatusUpdates { get; set; } = new List<StatusUpdate>();
}
