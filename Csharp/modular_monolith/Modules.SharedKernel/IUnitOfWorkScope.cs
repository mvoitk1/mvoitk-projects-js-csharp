namespace Modules.SharedKernel;

/// <summary>
/// An app-level transaction spanning the participating module DbContexts. The
/// modules share one physical database, so a single relational transaction lets
/// a use-case (e.g. checkout) commit writes from several modules atomically.
///
/// Persistence-free abstraction so the Application layer can depend on it; the
/// relational implementation lives in Modules.Infrastructure. When no
/// participating context is relational (e.g. the InMemory test provider) every
/// method is a safe no-op and each context simply saves on its own as before.
/// </summary>
public interface IUnitOfWorkScope : IAsyncDisposable
{
    /// <summary>Open the shared connection and begin one transaction the modules enlist in.</summary>
    Task BeginAsync(CancellationToken ct = default);

    /// <summary>Commit the transaction. No-op if none was started.</summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>Roll back the transaction. No-op if none was started.</summary>
    Task RollbackAsync(CancellationToken ct = default);
}
