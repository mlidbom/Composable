#region usings

using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerDocumentDbConfig : DocumentDbConfig
    {
        public new static readonly SqlServerDocumentDbConfig Default = new SqlServerDocumentDbConfig();

        public Formatting JSonFormatting = Formatting.None;
    }
}