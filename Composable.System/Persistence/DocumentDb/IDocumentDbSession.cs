namespace Composable.Persistence.DocumentDb
{
    //todo: break up and probably remove this monolithic interface.
    internal interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater
    {}
}