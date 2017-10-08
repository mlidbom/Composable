using Newtonsoft.Json;

namespace Composable.NewtonSoft
{
    static class JsonSettings
    {
        internal static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
            };

        public static readonly JsonSerializerSettings SqlEventStoreSerializerSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = IgnoreAggregateRootEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql.Instance
            };

    }
}
