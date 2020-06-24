using Newtonsoft.Json;

namespace Composable.Persistence.SqlServer.EventStore {
    static class SqlServerAggregateEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{SqlServerDebugEventStoreEventSerializer.Serialize(@this, formatting)}";
    }
}