using Newtonsoft.Json;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer {
    static class AggregateEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{SqlServerDebugEventStoreEventSerializer.Serialize(@this, formatting)}";
    }
}