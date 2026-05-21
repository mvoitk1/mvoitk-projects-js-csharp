using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Modules.Infrastructure;

/// <summary>
/// Wraps a single module DbContext so the transaction coordinator can enlist it
/// without knowing its concrete type.
/// </summary>
public sealed class DbContextTransactionParticipant<TContext>(TContext context) : ITransactionParticipant
    where TContext : DbContext
{
    public bool IsRelational => context.Database.IsRelational();

    public void Enlist(DbTransaction transaction) => context.Database.UseTransaction(transaction);
}
