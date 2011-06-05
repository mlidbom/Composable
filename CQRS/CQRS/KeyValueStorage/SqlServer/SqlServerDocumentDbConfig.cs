#region usings

using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerDocumentDbConfig : DocumentDbConfig
    {
        public static readonly SqlServerDocumentDbConfig Default = new SqlServerDocumentDbConfig();

        public bool Batching = true;
        public Formatting JSonFormatting = Formatting.None;
    }
}