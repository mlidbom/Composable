namespace Composable.CQRS
{
    public interface IEntityFetcher
    {
        TEntity Get<TEntity>(object entityId);
    }
}