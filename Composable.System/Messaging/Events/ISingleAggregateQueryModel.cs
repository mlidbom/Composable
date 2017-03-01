namespace Composable.Messaging.Events
{
  using Composable.DDD;

  using global::System;

  public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
    {
        void SetId(Guid id);
    }
}