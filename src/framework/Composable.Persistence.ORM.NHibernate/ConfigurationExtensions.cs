using System.Collections.Generic;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;

namespace Composable.Persistence.ORM.NHibernate
{
    public static class ConfigurationExtensions
    {
        public static void AddCodeMappingsFromAssemblies(this Configuration @this, IEnumerable<Assembly> assemblies)
        {
            var mapper = new ModelMapper();
            foreach (var mappingAssembly in assemblies)
            {
                mapper.AddMappings(mappingAssembly.GetTypes());
            }

            @this.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }
}