namespace Composable.Persistence.DocumentDb
{
    //todo: break up and probably remove this monolithic interface.
    interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater
    {}
}