using Newtonsoft.Json;

namespace Composable.Testing
{
    static class AggregateEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented) => $"{@this.GetType()}:{DebugEventStoreEventSerializer.Serialize(@this, formatting)}";
    }
}