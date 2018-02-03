namespace Composable.Persistence.DocumentDb
{
    //refactor: break up and probably remove this monolithic interface.
    interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater
    {}
}