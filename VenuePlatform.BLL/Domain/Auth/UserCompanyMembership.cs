namespace VenuePlatform.BLL.Domain.Auth;

/// <summary>
/// Join entity linking Identity users to companies with a tenant-scoped role.
/// A user can belong to multiple companies with different roles per company.
/// </summary>
public sealed class UserCompanyMembership
{
    public Guid UserId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Role { get; private set; } = null!;
    public DateTime CreatedUtc { get; private set; }

    private UserCompanyMembership() { } // EF

    public UserCompanyMembership(Guid userId, Guid companyId, string role)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId is required.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));

        UserId = userId;
        CompanyId = companyId;
        Role = role.Trim();
        CreatedUtc = DateTime.UtcNow;
    }
}
