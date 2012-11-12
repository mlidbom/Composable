using Newtonsoft.Json;

namespace Composable.NewtonSoft
{
    public static class JsonSettings
    {
        public static JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                return new JsonSerializerSettings
                       {
                           TypeNameHandling = TypeNameHandling.Auto,
                           ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                           ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                       };
            }
        }
                  
    }
}
