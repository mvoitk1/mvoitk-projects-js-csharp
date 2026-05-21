using System.Data.Common;

namespace Modules.Infrastructure;

/// <summary>
/// A module's hook for joining an app-level transaction. Each module's
/// infrastructure registers one of these wrapping its own DbContext, so the
/// coordinator can enlist every module without referencing their context types.
/// </summary>
public interface ITransactionParticipant
{
    /// <summary>True when the wrapped context uses a relational provider (false for InMemory tests).</summary>
    bool IsRelational { get; }

    /// <summary>Enlist the wrapped context in the supplied transaction.</summary>
    void Enlist(DbTransaction transaction);

    /// <summary>
    /// Detach the wrapped context from any enlisted transaction. Must be called
    /// once the shared transaction completes, otherwise later queries on the
    /// context reuse the now-completed transaction and throw
    /// "Transaction is already completed".
    /// </summary>
    void Clear();
}
