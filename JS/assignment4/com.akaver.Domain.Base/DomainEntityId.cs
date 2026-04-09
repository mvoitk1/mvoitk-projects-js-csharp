using com.akaver.Contracts.Domain.Base;

namespace com.akaver.Domain.Base;

public abstract class DomainEntityId : DomainEntityId<Guid>, IDomainEntityId, IDomainEntityId<Guid>
{
}

public abstract class DomainEntityId<TKey> : IDomainEntityId<TKey> where TKey : IEquatable<TKey>
{
    public virtual TKey Id { get; set; } = default!;
}
