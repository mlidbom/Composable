using Newtonsoft.Json;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer {
    static class AggregateRootEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented)
        {
            return $"{@this.GetType()}:{SqlServerDebugEventStoreEventSerializer.Serialize(@this, formatting)}";
        }
    }
}