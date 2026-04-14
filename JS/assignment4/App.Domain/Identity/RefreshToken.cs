using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using com.akaver.Contracts.Domain.Base;
using com.akaver.Domain.Base;

namespace App.Domain.Identity;

public class RefreshToken: DomainEntityId, IDomainAppUser<AppUser>
{
    public Guid AppUserId { get; set; }
    [JsonIgnore]
    public AppUser? AppUser { get; set; }
    
    [StringLength(36, MinimumLength = 36)]
    public string Token { get; set; } = Guid.NewGuid().ToString();
    // UTC
    public DateTime TokenExpirationDateTime { get; set; } = DateTime.UtcNow.AddDays(7);

    [StringLength(36, MinimumLength = 36)]
    public string? PreviousToken { get; set; }
    // UTC
    public DateTime? PreviousTokenExpirationDateTime { get; set; }
}