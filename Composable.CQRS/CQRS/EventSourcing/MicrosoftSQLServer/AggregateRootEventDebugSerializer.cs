using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer {
    static class AggregateRootEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{SqlServerDebugEventStoreEventSerializer.Serialize(@this, formatting)}";
    }
}