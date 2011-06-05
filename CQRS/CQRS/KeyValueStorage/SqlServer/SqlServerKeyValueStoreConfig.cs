#region usings

using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueStoreConfig : KeyValueStoreConfig
    {
        public static readonly SqlServerKeyValueStoreConfig Default = new SqlServerKeyValueStoreConfig();

        public bool Batching = true;
        public Formatting JSonFormatting = Formatting.None;
    }
}