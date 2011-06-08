namespace Composable.KeyValueStorage.SqlServer
{
    public class InMemoryDocumentDbConfig : DocumentDbConfig
    {
        public static readonly InMemoryDocumentDbConfig Default = new InMemoryDocumentDbConfig();
    }
}