using Newtonsoft.Json;

namespace Composable.NewtonSoft
{
    public static class JsonSettings
    {
// ReSharper disable InconsistentNaming
        private static readonly JsonSerializerSettings _jsonSerializerSettings =
// ReSharper restore InconsistentNaming
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
            };

        public static JsonSerializerSettings JsonSerializerSettings { get { return _jsonSerializerSettings; } }
    }
}
