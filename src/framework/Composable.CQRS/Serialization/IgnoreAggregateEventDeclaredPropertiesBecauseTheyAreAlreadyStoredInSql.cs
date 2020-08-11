using System.Reflection;
using Composable.Persistence.EventStore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.Serialization
{
    class IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IncludeMembersWithPrivateSettersResolver, IStaticInstancePropertySingleton
    {
        public new static readonly IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql Instance = new IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql();
        IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql() {
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if(property.DeclaringType == typeof(AggregateEvent))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}