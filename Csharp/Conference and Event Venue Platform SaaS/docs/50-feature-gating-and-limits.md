# 50 - Feature Gating and Limits

## Subscription Tiers

| Feature | Free | Standard | Premium |
|---------|------|----------|---------|
| **Users** | 2 | 10 | Unlimited |
| **Spaces** | 3 | 20 | Unlimited |
| **Bookings/month** | 10 | 100 | Unlimited |
| **Equipment items** | 10 | 50 | Unlimited |
| **Catering options** | 5 | 20 | Unlimited |
| **Recurring templates** | 0 | 5 | Unlimited |
| **Multi-venue** | ❌ | ❌ | ✅ |
| **Custom branding** | ❌ | ✅ | ✅ |
| **API access** | ❌ | ❌ | ✅ |
| **Priority support** | ❌ | ✅ | ✅ |
| **Advanced reporting** | ❌ | ❌ | ✅ |

---

## Tier Definitions

```csharp
public enum SubscriptionTier
{
    Free,
    Standard,
    Premium
}

public static class TierLimits
{
    public static TierLimit GetLimits(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => new TierLimit
        {
            MaxUsers = 2,
            MaxSpaces = 3,
            MaxBookingsPerMonth = 10,
            MaxEquipmentItems = 10,
            MaxCateringOptions = 5,
            MaxRecurringTemplates = 0,
            AllowMultiVenue = false,
            AllowCustomBranding = false,
            AllowApiAccess = false,
            PricePerMonth = 0
        },
        SubscriptionTier.Standard => new TierLimit
        {
            MaxUsers = 10,
            MaxSpaces = 20,
            MaxBookingsPerMonth = 100,
            MaxEquipmentItems = 50,
            MaxCateringOptions = 20,
            MaxRecurringTemplates = 5,
            AllowMultiVenue = false,
            AllowCustomBranding = true,
            AllowApiAccess = false,
            PricePerMonth = 49
        },
        SubscriptionTier.Premium => new TierLimit
        {
            MaxUsers = int.MaxValue,
            MaxSpaces = int.MaxValue,
            MaxBookingsPerMonth = int.MaxValue,
            MaxEquipmentItems = int.MaxValue,
            MaxCateringOptions = int.MaxValue,
            MaxRecurringTemplates = int.MaxValue,
            AllowMultiVenue = true,
            AllowCustomBranding = true,
            AllowApiAccess = true,
            PricePerMonth = 149
        },
        _ => throw new ArgumentOutOfRangeException()
    };
}

public class TierLimit
{
    public int MaxUsers { get; set; }
    public int MaxSpaces { get; set; }
    public int MaxBookingsPerMonth { get; set; }
    public int MaxEquipmentItems { get; set; }
    public int MaxCateringOptions { get; set; }
    public int MaxRecurringTemplates { get; set; }
    public bool AllowMultiVenue { get; set; }
    public bool AllowCustomBranding { get; set; }
    public bool AllowApiAccess { get; set; }
    public decimal PricePerMonth { get; set; }
}
```

---

## Feature Flags

```csharp
public enum FeatureType
{
    // Core features
    BasicBooking,
    ClientManagement,
    EquipmentTracking,
    CateringManagement,
    Invoicing,
    
    // Advanced features
    RecurringBookings,
    MultiVenue,
    CustomBranding,
    ApiAccess,
    AdvancedReporting,
    SplitBilling,
    
    // Limits
    UserLimit,
    SpaceLimit,
    BookingLimit,
    EquipmentLimit
}

public static class FeatureTiers
{
    public static IReadOnlySet<FeatureType> GetFeatures(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => FreeFeatures,
        SubscriptionTier.Standard => StandardFeatures,
        SubscriptionTier.Premium => PremiumFeatures,
        _ => new HashSet<FeatureType>()
    };
    
    private static readonly IReadOnlySet<FeatureType> FreeFeatures = new HashSet<FeatureType>
    {
        FeatureType.BasicBooking,
        FeatureType.ClientManagement,
        FeatureType.EquipmentTracking,
        FeatureType.CateringManagement,
        FeatureType.Invoicing,
        FeatureType.UserLimit,
        FeatureType.SpaceLimit,
        FeatureType.BookingLimit,
        FeatureType.EquipmentLimit
    };
    
    private static readonly IReadOnlySet<FeatureType> StandardFeatures = new HashSet<FeatureType>
    {
        FeatureType.BasicBooking,
        FeatureType.ClientManagement,
        FeatureType.EquipmentTracking,
        FeatureType.CateringManagement,
        FeatureType.Invoicing,
        FeatureType.RecurringBookings,
        FeatureType.CustomBranding,
        FeatureType.SplitBilling,
        FeatureType.UserLimit,
        FeatureType.SpaceLimit,
        FeatureType.BookingLimit,
        FeatureType.EquipmentLimit
    };
    
    private static readonly IReadOnlySet<FeatureType> PremiumFeatures = new HashSet<FeatureType>
    {
        FeatureType.BasicBooking,
        FeatureType.ClientManagement,
        FeatureType.EquipmentTracking,
        FeatureType.CateringManagement,
        FeatureType.Invoicing,
        FeatureType.RecurringBookings,
        FeatureType.MultiVenue,
        FeatureType.CustomBranding,
        FeatureType.ApiAccess,
        FeatureType.AdvancedReporting,
        FeatureType.SplitBilling,
        FeatureType.UserLimit,
        FeatureType.SpaceLimit,
        FeatureType.BookingLimit,
        FeatureType.EquipmentLimit
    };
}
```

---

## Feature Checker Service

### Dependency Direction

- `IFeatureChecker` interface lives in **BLL** (VenuePlatform.BLL)
- Implementation lives in **DAL** or **Web** layer
- **BLL must NOT reference DbContext directly**
- Access current user via `ICurrentUser` abstraction (defined in BLL, implemented in Web)

```csharp
// BLL - Application/Common/Interfaces/IFeatureChecker.cs
public interface IFeatureChecker
{
    Task<bool> IsEnabledAsync(FeatureType feature);
    Task<bool> IsEnabledAsync(FeatureType feature, Guid companyId);
    Task<TierLimit> GetCurrentLimitsAsync();
    Task<TierLimit> GetCurrentLimitsAsync(Guid companyId);
    Task<bool> HasReachedLimitAsync(LimitType limitType);
    Task<int> GetCurrentUsageAsync(LimitType limitType);
}

// BLL - Application/Common/Interfaces/ICurrentUser.cs
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdmin { get; }
}

// DAL - Services/FeatureChecker.cs
public class FeatureChecker : IFeatureChecker
{
    private readonly ITenantContext _tenantContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    
    public FeatureChecker(
        ITenantContext tenantContext,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _tenantContext = tenantContext;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }
    
    public async Task<bool> IsEnabledAsync(FeatureType feature)
    {
        var companyId = _tenantContext.CurrentCompanyId
            ?? throw new InvalidOperationException("No company context");
        return await IsEnabledAsync(feature, companyId);
    }
    
    public async Task<bool> IsEnabledAsync(FeatureType feature, Guid companyId)
    {
        var company = await _dbContext.Companies.FindAsync(companyId);
        if (company == null) return false;
        
        // System admins bypass all feature checks
        if (_currentUser.IsSystemAdmin) return true;
        
        var features = FeatureTiers.GetFeatures(company.Tier);
        return features.Contains(feature);
    }
    
    public async Task<bool> HasReachedLimitAsync(LimitType limitType)
    {
        var companyId = _companyContext.CurrentCompanyId 
            ?? throw new InvalidOperationException("No company context");
            
        var company = await _dbContext.Companies.FindAsync(companyId);
        var limits = TierLimits.GetLimits(company.Tier);
        var currentUsage = await GetCurrentUsageAsync(limitType);
        
        var maxAllowed = limitType switch
        {
            LimitType.Users => limits.MaxUsers,
            LimitType.Spaces => limits.MaxSpaces,
            LimitType.Bookings => limits.MaxBookingsPerMonth,
            LimitType.Equipment => limits.MaxEquipmentItems,
            _ => 0
        };
        
        return currentUsage >= maxAllowed;
    }
    
    public async Task<int> GetCurrentUsageAsync(LimitType limitType)
    {
        var companyId = _companyContext.CurrentCompanyId.Value;
        
        return limitType switch
        {
            LimitType.Users => await _dbContext.CompanyMemberships
                .CountAsync(m => m.CompanyId == companyId && m.IsActive),
                
            LimitType.Spaces => await _dbContext.Spaces
                .CountAsync(s => s.CompanyId == companyId && !s.IsDeleted),
                
            LimitType.Bookings => await _dbContext.Bookings
                .CountAsync(b => b.CompanyId == companyId 
                    && b.StartTime >= DateTime.UtcNow.AddMonths(-1)),
                    
            LimitType.Equipment => await _dbContext.Equipment
                .CountAsync(e => e.CompanyId == companyId && !e.IsDeleted),
                
            _ => 0
        };
    }
}

public enum LimitType
{
    Users,
    Spaces,
    Bookings,
    Equipment
}
```

---

## Feature Gate Attributes

### MVC Filter

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeatureAttribute : TypeFilterAttribute
{
    public RequireFeatureAttribute(FeatureType feature) 
        : base(typeof(RequireFeatureFilter))
    {
        Arguments = new object[] { feature };
    }
}

public class RequireFeatureFilter : IAsyncAuthorizationFilter
{
    private readonly FeatureType _feature;
    private readonly IFeatureChecker _featureChecker;
    
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var isEnabled = await _featureChecker.IsEnabledAsync(_feature);
        
        if (!isEnabled)
        {
            context.Result = new RedirectToActionResult(
                "Upgrade", "Subscription", null);
        }
    }
}
```

### Usage

```csharp
[Authorize]
[Route("{companySlug}/[controller]")]
public class RecurringBookingsController : Controller
{
    [RequireFeature(FeatureType.RecurringBookings)]
    public async Task<IActionResult> Index() { }
    
    [RequireFeature(FeatureType.RecurringBookings)]
    public async Task<IActionResult> Create() { }
}
```

---

## Limit Enforcement

### Create User Limit Example

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateUserCommand request, 
        CancellationToken cancellationToken)
    {
        // Check if feature is enabled
        if (!await _featureChecker.IsEnabledAsync(FeatureType.UserLimit))
        {
            return Result<Guid>.Failure("User management not available on this tier");
        }
        
        // Check if limit reached
        if (await _featureChecker.HasReachedLimitAsync(LimitType.Users))
        {
            var limits = await _featureChecker.GetCurrentLimitsAsync();
            var usage = await _featureChecker.GetCurrentUsageAsync(LimitType.Users);
            
            return Result<Guid>.Failure(
                $"User limit reached ({usage}/{limits.MaxUsers}). " +
                "Please upgrade your subscription.");
        }
        
        // Proceed with creation...
    }
}
```

---

## Self-Service Tenant Creation

### Company Creation Flow

```csharp
public class CreateCompanyCommand : IRequest<Result<CompanyDto>>
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public SubscriptionTier SelectedTier { get; set; }
}

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    public async Task<Result<CompanyDto>> Handle(
        CreateCompanyCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate slug uniqueness
        if (await _dbContext.Companies.AnyAsync(c => c.Slug == request.Slug))
        {
            return Result<CompanyDto>.Failure("Company slug already exists");
        }
        
        var company = new Company
        {
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant(),
            Tier = request.SelectedTier,
            IsActive = true
        };
        
        // Create default settings
        company.Settings = new CompanySettings
        {
            DefaultCateringLeadTimeHours = 72,
            DefaultCurrency = "EUR",
            DefaultTimeZone = "Europe/Tallinn"
        };
        
        // Create subscription
        company.Subscription = new Subscription
        {
            Tier = request.SelectedTier,
            StartDate = DateTime.UtcNow,
            Status = request.SelectedTier == SubscriptionTier.Free 
                ? SubscriptionStatus.Active 
                : SubscriptionStatus.Trial,
            TrialEndsAt = request.SelectedTier == SubscriptionTier.Free 
                ? null 
                : DateTime.UtcNow.AddDays(14),
            MonthlyPrice = TierLimits.GetLimits(request.SelectedTier).PricePerMonth
        };
        
        // Add current user as owner
        var membership = new CompanyMembership
        {
            UserId = _currentUser.UserId,
            Role = TenantRole.CompanyOwner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        company.Memberships.Add(membership);
        
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();
        
        return Result<CompanyDto>.Success(_mapper.Map<CompanyDto>(company));
    }
}
```

---

## Upgrade/Downgrade Handling

```csharp
public class ChangeSubscriptionTierCommand : IRequest<Result>
{
    public Guid CompanyId { get; set; }
    public SubscriptionTier NewTier { get; set; }
}

public class ChangeSubscriptionTierCommandHandler : IRequestHandler<ChangeSubscriptionTierCommand, Result>
{
    public async Task<Result> Handle(ChangeSubscriptionTierCommand request, CancellationToken cancellationToken)
    {
        var company = await _dbContext.Companies
            .Include(c => c.Memberships)
            .Include(c => c.Spaces)
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId);
            
        if (company == null)
            return Result.Failure("Company not found");
            
        var newLimits = TierLimits.GetLimits(request.NewTier);
        
        // Check if current usage exceeds new tier limits
        var currentUsers = company.Memberships.Count(m => m.IsActive);
        if (currentUsers > newLimits.MaxUsers)
        {
            return Result.Failure(
                $"Cannot downgrade: Current user count ({currentUsers}) " +
                $"exceeds new tier limit ({newLimits.MaxUsers}). " +
                "Please remove users first.");
        }
        
        var currentSpaces = company.Spaces.Count(s => !s.IsDeleted);
        if (currentSpaces > newLimits.MaxSpaces)
        {
            return Result.Failure(
                $"Cannot downgrade: Current space count ({currentSpaces}) " +
                $"exceeds new tier limit ({newLimits.MaxSpaces}).");
        }
        
        // Handle disabled features
        if (!newLimits.AllowMultiVenue && company.HasMultipleVenues)
        {
            return Result.Failure(
                "Cannot downgrade: Please consolidate venues first.");
        }
        
        // Apply tier change
        company.Tier = request.NewTier;
        company.Subscription.Tier = request.NewTier;
        company.Subscription.MonthlyPrice = newLimits.PricePerMonth;
        
        await _dbContext.SaveChangesAsync();
        
        return Result.Success();
    }
}
```

---

## UI Feature Indicators

### ViewModel Helper

```csharp
public class FeatureViewModel
{
    public bool IsEnabled { get; set; }
    public bool HasReachedLimit { get; set; }
    public int CurrentUsage { get; set; }
    public int MaxAllowed { get; set; }
    public string UpgradeUrl { get; set; } = "/subscription/upgrade";
}

// In controller
public async Task<IActionResult> Index()
{
    var viewModel = new DashboardViewModel
    {
        RecurringBookingsFeature = new FeatureViewModel
        {
            IsEnabled = await _featureChecker.IsEnabledAsync(FeatureType.RecurringBookings),
            HasReachedLimit = await _featureChecker.HasReachedLimitAsync(LimitType.Bookings),
            CurrentUsage = await _featureChecker.GetCurrentUsageAsync(LimitType.Bookings),
            MaxAllowed = (await _featureChecker.GetCurrentLimitsAsync()).MaxBookingsPerMonth
        }
    };
    
    return View(viewModel);
}
```

### Razor View Usage

```html
@if (Model.RecurringBookingsFeature.IsEnabled)
{
    <a asp-action="CreateRecurring" class="btn btn-primary">
        Create Recurring Booking
    </a>
}
else
{
    <button class="btn btn-secondary" disabled>
        <i class="bi bi-lock"></i> 
        Recurring Bookings (Standard+)
    </button>
    <a href="@Model.RecurringBookingsFeature.UpgradeUrl" class="btn btn-link">
        Upgrade to enable
    </a>
}

@if (Model.UsersFeature.HasReachedLimit)
{
    <div class="alert alert-warning">
        User limit reached (@Model.UsersFeature.CurrentUsage/@Model.UsersFeature.MaxAllowed).
        <a href="/subscription/upgrade">Upgrade</a> to add more users.
    </div>
}
```
