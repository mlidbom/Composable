using System.Reflection;
using Composable.CQRS.EventSourcing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.CQRS.NewtonSoft
{
    class IgnoreAggregateRootEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IncludeMembersWithPrivateSettersResolver
    {
        public new static readonly IgnoreAggregateRootEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql Instance = new IgnoreAggregateRootEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql();
        IgnoreAggregateRootEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql() {
        }
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if(property.DeclaringType == typeof(AggregateRootEvent))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}