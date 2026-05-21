# Cross-Module Transactional Consistency — Option A Plan

Scope: make the checkout flow (`Sales.PlaceOrderAsync` + `Catalog.ReserveStock`)
strongly consistent by committing both modules' writes in **one database
transaction**, and remove the fragile compensation logic. Also fixes two
concrete correctness bugs in the current flow.

This is the "Option A — shared transaction" direction: the three modules share
one physical Postgres database (`DefaultConnection` → `webapp2526s_a3`), so a
single local transaction is available and is simpler and safer than an
outbox/saga. The MediatR contracts in `Modules.Contracts` stay exactly as they
are — they remain the seam to cut along if the module is ever extracted into a
service (at which point this plan would be replaced by an outbox + saga).

Items are ordered to be done top-down; build + run tests after each.

---

## Why we're doing this

Current `PlaceOrderAsync` ([Sales.Application/Services/OrderService.cs](../Sales.Application/Services/OrderService.cs)):

1. `ReserveStockCommand` → Catalog decrements stock and **commits immediately**.
2. Build order, save → Sales **commits separately**.
3. On exception, `ReleaseStockCommand` compensates.

Three problems:

- **Bug 1 — post-commit publish can wrongly release stock.**
  `publisher.Publish(OrderPlacedEvent)` runs *inside the try, after* the order
  is committed. MediatR notifications run synchronously; if any handler throws,
  control jumps to the `catch`, which releases stock for an order that is
  already persisted and `Confirmed`.
- **Bug 2 — read-then-write race in stock decrement.** Two concurrent checkouts
  can both read `StockQty = 5`, both pass the check, both decrement → oversell.
- **Bug 3 — crash window.** Stock is committed before the order; a process
  crash between the two leaks stock with no recovery record. (A shared
  transaction eliminates this entirely — there is nothing to compensate.)

---

## 1. Share one DbConnection across the module DbContexts [PREREQUISITE]

Two `DbContext` instances can enlist in the same transaction only if they share
the same underlying `DbConnection`. Today each module's infrastructure builds
its own connection from the string.

**Files:**
- [Catalog.Infrastructure/CatalogInfrastructureServiceCollectionExtensions.cs](../Catalog.Infrastructure/CatalogInfrastructureServiceCollectionExtensions.cs)
- [Sales.Infrastructure/SalesInfrastructureServiceCollectionExtensions.cs](../Sales.Infrastructure/SalesInfrastructureServiceCollectionExtensions.cs)
- [Users.Infrastructure/UsersInfrastructureServiceCollectionExtensions.cs](../Users.Infrastructure/UsersInfrastructureServiceCollectionExtensions.cs)
  (Users does not participate in checkout, but must use the same connection
  registration so the scoped connection is consistent app-wide.)

**Change:** register a single **scoped** `NpgsqlConnection` and configure each
context against it instead of the raw string.

```csharp
// Register once (shared infra helper, see step 2). Each module's Add* then does:
services.AddDbContext<CatalogDbContext>((sp, options) =>
{
    var connection = sp.GetRequiredService<NpgsqlConnection>();
    options.UseNpgsql(connection, npgsql =>
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Catalog"));
});
```

Notes:
- Connection lifetime = **scoped** (one per request), so all contexts in a
  request share the exact same `DbConnection` instance.
- Keep per-module migration history tables (separate `MigrationsHistoryTable`)
  so each module keeps owning its own migrations — logical isolation is
  preserved.
- Verify the InMemory test path still works: the shared-connection registration
  must only apply on the Npgsql path. Guard it the same way the existing
  `DataInitialization`/InMemory checks are guarded (see `SeedAsync` in the
  `*.Module` classes and `Tests.Shared`).

**Verify:** `dotnet build`; existing integration tests under
`WebApp.Tests` / `Modules.*.Tests` still pass.

---

## 2. Add a decoupled transaction coordinator [CORE]

Goal: let `PlaceOrderAsync` wrap "reserve stock (Catalog) + write order (Sales)"
in one transaction **without** Sales referencing Catalog's `DbContext` (that
would break the module boundary the architecture is built on).

Use the same self-registration philosophy as `IModule`.

**New file:** `Modules.Abstractions/ITransactionParticipant.cs`

```csharp
namespace Modules.Abstractions;

/// A module's hook to enlist its DbContext in an app-level transaction.
public interface ITransactionParticipant
{
    void EnlistInCurrentTransaction(System.Data.Common.DbTransaction transaction);
}
```

**New file:** `Modules.Abstractions/IUnitOfWorkScope.cs`

```csharp
namespace Modules.Abstractions;

/// Begins one DB transaction on the shared connection and enlists every
/// registered ITransactionParticipant. Commit/rollback covers all modules.
public interface IUnitOfWorkScope : IAsyncDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

**Implementation** (in `Modules.SharedKernel` or a new tiny
`Modules.Infrastructure` project — must NOT reference any concrete module):
- Resolve the scoped `NpgsqlConnection` + `IEnumerable<ITransactionParticipant>`.
- `BeginAsync`: open connection if needed, `BeginTransaction()`, then call
  `EnlistInCurrentTransaction(tx)` on every participant.
- `CommitAsync` / `RollbackAsync`: commit/rollback the single transaction.

**Each module's infrastructure** registers a participant that wraps its own
context, e.g. in Sales/Catalog `Add*Infrastructure`:

```csharp
services.AddScoped<ITransactionParticipant>(sp =>
    new DbContextTransactionParticipant(sp.GetRequiredService<SalesDbContext>()));
```

where `DbContextTransactionParticipant.EnlistInCurrentTransaction` calls
`dbContext.Database.UseTransaction(transaction)`.

**Register** the coordinator in the composition root (`WebApp`) or as part of a
shared `AddModulesCore()` extension called from `Program.cs`.

> **Lighter alternative** if the participant indirection feels heavy: let the
> coordinator live in `WebApp` (the composition root is *allowed* to know all
> contexts) and call `UseTransaction` on each context directly. It's less pure
> but fewer types. Prefer the participant approach to keep modules
> self-contained.

**Verify:** unit-test the coordinator with two contexts on a shared connection:
begin → write in both → rollback → assert neither write persisted; then
begin → write → commit → assert both persisted.

---

## 3. Make `PlaceOrderAsync` use the shared transaction [CORE]

**File:** [Sales.Application/Services/OrderService.cs](../Sales.Application/Services/OrderService.cs)

Inject `IUnitOfWorkScope` into `OrderService`. Restructure:

```csharp
await using var tx = uowScope;            // injected
await tx.BeginAsync();
try
{
    var reservation = await mediator.Send(new ReserveStockCommand(lines));
    if (!reservation.Success)
    {
        await tx.RollbackAsync();
        throw new InvalidOperationException($"Insufficient stock for variants: ...");
    }

    var pricingMap = await mediator.Send(new GetVariantsPricingQuery(...));

    // build order, remove cart items, mark cart CheckedOut
    uow.Orders.Add(order);
    await uow.SaveChangesAsync();         // NO separate commit — same transaction

    await tx.CommitAsync();               // reserve + order commit atomically
}
catch
{
    await tx.RollbackAsync();             // stock decrement rolls back with it
    throw;
}

// Publish AFTER commit, outside the transaction (see step 4)
await PublishOrderPlacedSafely(order, userId, totalAmount);
```

Key changes:
- **`ReserveStockCommand` writes now participate in the same transaction** —
  `ReserveStockCommandHandler`'s `SaveChangesAsync` no longer commits on its
  own; it flushes into the shared transaction (because both contexts share the
  connection + enlisted transaction).
- **Delete the `ReleaseStockCommand` compensation path** in `PlaceOrderAsync` —
  rollback handles it. (Keep `ReleaseStockCommand` + handler in the codebase
  only if used elsewhere; otherwise mark for removal.)
- The `GetVariantsPricingQuery` is a read; it's fine inside or outside the tx.

**Caveat to confirm during implementation:** because `ReserveStock` no longer
self-commits, its `SaveChangesAsync` must run on the *same scoped connection +
transaction*. This is guaranteed by steps 1–2 (shared scoped connection, all
contexts enlisted at `BeginAsync`). Add an assertion/log if the handler runs
without an active transaction, to catch misconfiguration early.

---

## 4. Fix Bug 1 — publish OrderPlacedEvent outside the transaction/compensation [HIGH]

**File:** [Sales.Application/Services/OrderService.cs](../Sales.Application/Services/OrderService.cs#L78-L92)

Move `publisher.Publish(OrderPlacedEvent)` to **after** `CommitAsync()` and
outside the `try/catch` that controls rollback, so a notification-handler
failure can never roll back / release stock for a committed order.

A failed downstream handler must not fail the checkout response. Wrap publish:

```csharp
private async Task PublishOrderPlacedSafely(Order order, Guid userId, decimal total)
{
    try { await publisher.Publish(new OrderPlacedEvent(...)); }
    catch (Exception ex) { logger.LogError(ex, "OrderPlacedEvent handler failed for {OrderId}", order.Id); }
}
```

(If a downstream side-effect *must* be reliable, that's the one case for an
outbox later — out of scope here. For now, log-and-continue is correct: the
order is committed and authoritative.)

**Verify:** add a test with a throwing `INotification` handler for
`OrderPlacedEvent`; assert the order still persists and stock stays decremented.

---

## 5. Fix Bug 2 — atomic conditional stock decrement [HIGH]

**File:** [Catalog.Application/Handlers/ReserveStockCommandHandler.cs](../Catalog.Application/Handlers/ReserveStockCommandHandler.cs)

The current read-check-decrement is not safe under concurrency even inside a
transaction (the read takes no lock). Pick one:

**Option 5a — optimistic concurrency token (preferred, EF-native):**
- Add a `rowversion`/`xmin` concurrency token to `ProductVariant` (Postgres:
  map the system `xmin` column as a concurrency token; no schema migration
  needed for the column itself, but a model + snapshot change is).
- On `DbUpdateConcurrencyException`, retry the reserve a bounded number of times.

**Option 5b — atomic conditional UPDATE:**
- Replace the in-memory decrement with a guarded update per line:
  `UPDATE "ProductVariants" SET "StockQty" = "StockQty" - @q
   WHERE "Id" = @id AND "IsActive" AND "StockQty" >= @q;`
- Treat **rows-affected = 0** as insufficient stock → add to the insufficient
  list. This makes check-and-decrement a single atomic statement.

Either way the handler still returns `ReserveStockResult(false, insufficientIds)`
on shortage so step 3 rolls back cleanly. Add a `SELECT ... FOR UPDATE` /
ordered access if deadlocks appear under load (order lines by variant id).

**Verify:** concurrency test — fire N parallel `PlaceOrderAsync` for the same
variant with stock = N; assert exactly N succeed, `StockQty` ends at 0, never
negative.

---

## 6. Migrations & cleanup

- If step 5a adds a concurrency token: generate a Catalog migration
  (`dotnet ef migrations add AddProductVariantConcurrencyToken --project Catalog.Infrastructure --startup-project WebApp`).
- Remove now-dead compensation code (`ReleaseStockCommand` send in
  `PlaceOrderAsync`; the command/handler themselves only if unused elsewhere —
  grep first).
- Update the `PlaceOrderAsync` XML doc comment: it currently describes the
  reserve-then-compensate flow, which no longer exists.

**Verify:** `grep -rn "ReleaseStockCommand" --include=*.cs .` returns only
intended remaining usages (ideally none in `PlaceOrderAsync`).

---

## Test checklist (run after all steps)

- [ ] `dotnet build` clean.
- [ ] Existing `WebApp.Tests` / `Modules.*.Tests` green.
- [ ] New: rollback test — order-write failure leaves stock unchanged.
- [ ] New: throwing `OrderPlacedEvent` handler — order persists, stock stays.
- [ ] New: concurrency test — no oversell, stock never negative.
- [ ] `WebApp.Tests.Postgres` (real Npgsql) green — confirms shared connection +
      transaction works against Postgres, not just InMemory.

---

## What this explicitly does NOT do

- No outbox, no saga, no durable messaging. The publish in step 4 is
  best-effort log-and-continue.
- No reservation lifecycle (hold + expiry). "Reserve" remains an immediate
  decrement — but now atomic and transactional.
- Does not change any `Modules.Contracts` types. The cross-module API surface
  is unchanged, preserving the extraction seam.

If/when a module must become a separate service, revisit with the
"Option B — outbox + saga" approach instead.
