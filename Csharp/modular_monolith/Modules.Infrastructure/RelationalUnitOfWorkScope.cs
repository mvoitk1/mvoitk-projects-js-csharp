using System.Data;
using System.Data.Common;
using Modules.SharedKernel;

namespace Modules.Infrastructure;

/// <summary>
/// Begins one transaction on the shared <see cref="DbConnection"/> and enlists
/// every relational <see cref="ITransactionParticipant"/>. All participating
/// module contexts share the same scoped connection, so their writes commit or
/// roll back together.
///
/// When no participant is relational (InMemory test provider) the scope is a
/// no-op: nothing is opened and each context saves independently, preserving the
/// previous test behaviour.
/// </summary>
public sealed class RelationalUnitOfWorkScope(
    DbConnection connection,
    IEnumerable<ITransactionParticipant> participants) : IUnitOfWorkScope
{
    private DbTransaction? _transaction;
    private bool _active;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction has already been started on this scope.");

        var relational = participants.Where(p => p.IsRelational).ToList();
        if (relational.Count == 0)
        {
            _active = false; // InMemory/tests: nothing to coordinate.
            return;
        }

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        _transaction = await connection.BeginTransactionAsync(ct);
        foreach (var participant in relational)
            participant.Enlist(_transaction);

        _active = true;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!_active || _transaction is null) return;
        await _transaction.CommitAsync(ct);
        await DisposeTransactionAsync();
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!_active || _transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await DisposeTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Safety net: an undisposed transaction means neither Commit nor
        // Rollback ran (e.g. an exception escaped) — roll back rather than leak.
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        _active = false;
    }
}
