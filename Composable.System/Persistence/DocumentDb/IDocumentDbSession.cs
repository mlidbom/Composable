namespace Composable.Persistence.DocumentDb
{
    //todo: break up and probably remove this monolithic interface.
    public interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater
    {}
}