namespace Composable.KeyValueStorage.SqlServer
{
    public class InMemoryDocumentDbConfig : DocumentDbConfig
    {
        public new static readonly InMemoryDocumentDbConfig Default = new InMemoryDocumentDbConfig();
    }
}