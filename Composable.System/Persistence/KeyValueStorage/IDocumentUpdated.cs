namespace Composable.Persistence.KeyValueStorage
{
    public interface IDocumentUpdated<out TDocument>
    {
        string Key { get; }
        TDocument Document { get; }
    }

    public interface IDocumentUpdated : IDocumentUpdated<object>
    {
    }
}