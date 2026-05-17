# Fix Plan — Outstanding Issues After Onion Cleanup

Scope: everything still flagged by audit after the onion-architecture refactor
(commits `98321e8`, `44a45b9`). Items are ordered by severity — do them top-down,
build + tests after each.

---

## 1. Security — plaintext password in failed-login log [HIGH]

**File:** [WebApp/ApiControllers/Identity/AccountController.cs:97](WebApp/ApiControllers/Identity/AccountController.cs#L97)

The failed-login warning currently writes the submitted password into the log
sink:

```csharp
_logger.LogWarning("WebApi login failed, password {} for email {} was wrong",
    loginInfo.Password, loginInfo.Email);
```

Anyone with log access (file, Seq, GitLab CI artefacts, journalctl) can harvest
credentials of failed-login attempts — which often include typos of valid
passwords.

**Fix:** drop the password parameter; keep only the email.

```csharp
_logger.LogWarning("WebApi login failed, password wrong for email {Email}",
    loginInfo.Email);
```

While editing, also fix the `{}`-without-name placeholders in the surrounding
`LogWarning`/`LogInformation` calls in the same file (structured logging
expects named tokens, e.g. `{Email}`).

**Verify:** grep `loginInfo.Password` should return zero hits in `WebApp/`.

---

## 2. Security — bump vulnerable NuGet packages [HIGH]

`dotnet build` currently reports:

- 8× NU1904 — `Microsoft.AspNetCore.DataProtection 10.0.1`
  ([GHSA-9mv3-2cwr-p262](https://github.com/advisories/GHSA-9mv3-2cwr-p262), critical)
- 16× NU1903 — `System.Security.Cryptography.Xml 10.0.1`
  ([GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx),
  [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf), high)
- 16× NU1901 — `NuGet.Packaging`/`NuGet.Protocol 6.12.1` (low)

**Files:**
- [WebApp/WebApp.csproj](WebApp/WebApp.csproj) — has `DataProtection.EntityFrameworkCore` 10.0.1
- [App.DAL.EF/App.DAL.EF.csproj](App.DAL.EF/App.DAL.EF.csproj) — transitive via Identity
- [WebApp.Tests/WebApp.Tests.csproj](WebApp.Tests/WebApp.Tests.csproj) — transitive

**Fix:** run `dotnet list package --vulnerable --include-transitive` to confirm
the chain, then bump to the latest patched 10.0.x (or add a direct
`<PackageReference>` if the offending package is transitive only). After the
bump:

```bash
dotnet restore
dotnet build  -c Debug   # NU190x count should drop to 0
dotnet test   WebApp.Tests/WebApp.Tests.csproj
```

---

## 3. Security hygiene — replace `new Random()` [LOW]

**File:** [WebApp/ApiControllers/Identity/AccountController.cs:35](WebApp/ApiControllers/Identity/AccountController.cs#L35)

```csharp
private readonly Random _random = new Random();
```

This is only used to jitter the failed-login delay (timing-attack
obfuscation), not for security primitives — so it's not a real
vulnerability — but the modern idiom is `Random.Shared`, which avoids
per-instance allocation and thread-affinity.

**Fix:**

1. Delete the `_random` field.
2. Replace `_random.Next(RandomDelayMin, RandomDelayMax)` with
   `Random.Shared.Next(RandomDelayMin, RandomDelayMax)` (two call sites).

---

## 4. Dead code — unused `_roleManager` in `UsersController` [LOW]

**File:** [WebApp/Areas/Root/Controllers/UsersController.cs:23](WebApp/Areas/Root/Controllers/UsersController.cs#L23)

`RoleManager<AppRole>` is injected and stored on the field, but never read.
`RoleRemove` / `RoleAdd` both go through `UserManager.RemoveFromRoleAsync` /
`AddToRoleAsync`, so the field is pure dead weight.

**Fix:** drop the field, the constructor parameter, and the `using` for
`RoleManager` if it becomes unused.

```csharp
public UsersController(ILogger<UsersController> logger,
                       UserManager<AppUser> userManager)
{
    _logger = logger;
    _userManager = userManager;
}
```

The corresponding view ([Areas/Root/Views/Users/Index.cshtml](WebApp/Areas/Root/Views/Users/Index.cshtml))
still `@inject`s `RoleManager<AppRole>` to render the role list — that one is
actually used, leave it alone.

---

## 5. Cruft — stale `TODO` in `AppDataInitExtensions` [LOW]

**File:** [WebApp/Setup/AppDataInitExtensions.cs:62](WebApp/Setup/AppDataInitExtensions.cs#L62)

```csharp
// TODO: Login failed for user 'sa'. Reason: Failed to open the explicitly specified database 'XYZ'. [CLIENT: 172.18.0.3]
```

References a SQL Server `sa` account and a deployment IP. The current stack
uses PostgreSQL — this is a leftover from an older host, not an actionable
TODO.

**Fix:** delete the comment.

---

## 6. Defensive — add `asp-area=""` to non-shared view links [LOW]

**Files:** every `<a asp-controller="…">` in
[WebApp/Views/Shop/](WebApp/Views/Shop/),
[WebApp/Views/Home/](WebApp/Views/Home/),
[WebApp/Views/Cart/](WebApp/Views/Cart/),
[WebApp/Views/Checkout/](WebApp/Views/Checkout/).

These do **not** cause bugs today: those views only render under the default
area, so the tag helper sees no ambient `area` route value and produces the
correct URL. The same omission in [WebApp/Views/Shared/Components/CartBadge/Default.cshtml](WebApp/Views/Shared/Components/CartBadge/Default.cshtml)
*did* cause the 404 we just fixed (`44a45b9`) because shared components
inherit the calling page's area.

**Fix:** as a defensive consistency pass — `[Layout.cshtml] already follows
this pattern — add `asp-area=""` to every cross-controller link in these views
so future refactors (moving a view, calling a partial cross-area) can't
reintroduce the same 404. Get an audit pass:

```bash
grep -rn 'asp-controller=' WebApp/Views/ | grep -v 'asp-area'
```

Should return nothing when done.

---

## 7. Cosmetic — XML doc `<param>` warnings (CS1573) [LOWEST]

18 occurrences across `WebApp/ApiControllers/v1/`. Each is a missing
`<param name="dto">` on an action where the existing XML comment already
documents the other parameters. Cosmetic only — Swagger ignores them.

**Fix:** add the missing `<param>` lines, or globally suppress CS1573 in
[WebApp/WebApp.csproj](WebApp/WebApp.csproj) (it already suppresses
CS1591 via `<NoWarn>$(NoWarn);1591</NoWarn>`).

Recommend: just add the missing `<param>` tags — it's a 5-minute mechanical
fix and keeps the Swagger doc complete.

---

## Out of scope (will not fix in this pass)

- **Identity-layer App.Domain imports** —
  [AccountController](WebApp/ApiControllers/Identity/AccountController.cs) and
  [Areas/Root/UsersController](WebApp/Areas/Root/Controllers/UsersController.cs)
  still import `App.Domain.Identity` because `UserManager<AppUser>` /
  `SignInManager<AppUser>` / `RoleManager<AppRole>` need the concrete type as
  a generic parameter. Documented exception per
  [onion_plan.md](onion_plan.md) §1.
- **Phase-1 deferred features** — payments, product reviews, multi-address
  address book, email notifications, image-upload (URLs only) — see
  [DOCUMENTATION.md](DOCUMENTATION.md) "Deferred Features".

---

## Suggested commit shape

One commit per section (1–6), small and reviewable; section 7 can ride along
with whatever section ends up last. After all fixes:

```bash
dotnet build -c Debug      # 0 warnings, 0 errors
dotnet test  WebApp.Tests  # 65/65 passing
grep -rn loginInfo.Password WebApp/                          # empty
grep -rn 'asp-controller=' WebApp/Views/ | grep -v asp-area  # empty
```
