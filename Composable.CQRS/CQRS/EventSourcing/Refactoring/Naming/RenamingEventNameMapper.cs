using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    public class RenamingEventNameMapper : IEventNameMapper
    {
        public string GetName(Type eventType)
        {
            string name;
            if(_typeToNameMappings.TryGetValue(eventType, out name))
            {
                return name;
            }

            throw new Exception($"Failed to find event name for: {eventType}.");
        }

        public Type GetType(string eventTypeName)
        {
            Type type;
            if(_nameToTypeMappings.TryGetValue(eventTypeName, out type))
            {
                return type;
            }

            throw new Exception($"Failed to find a type for: {eventTypeName}");
        }

        public RenamingEventNameMapper(IEnumerable<Type> eventTypes, params IRenameEvents[] renamers)
        {
            var nameMappings = eventTypes
                .Where(type => type.Implements<IAggregateRootEvent>())
                .Select(type => new EventNameMapping(type))
                .ToArray();

            nameMappings.ForEach(mapping => renamers.ForEach(renamer => renamer.Rename(mapping)));

            AssertMappingsAreValid(nameMappings);

            _nameToTypeMappings = nameMappings.ToDictionary(
                keySelector: mapping => mapping.FullName,
                elementSelector: mapping => mapping.Type
                );

            _typeToNameMappings = nameMappings.ToDictionary(
                keySelector: mapping => mapping.Type,
                elementSelector: mapping => mapping.FullName
                );
        }

        private readonly Dictionary<string, Type> _nameToTypeMappings;
        private readonly Dictionary<Type, string> _typeToNameMappings;

        private static void AssertMappingsAreValid(EventNameMapping[] mappings)
        {
            var detectedDuplicate = mappings.GroupBy(mapping => mapping.FullName)
                                            .Where(grouping => grouping.Count() > 1)
                                            .FirstOrDefault();

            if(detectedDuplicate != null)
            {
                throw new Exception(
                    $@"Duplicate event name detected: 
Name: {detectedDuplicate.Key}
Claimed by:
    {
                        detectedDuplicate.ToArray().Select(mapping => mapping.Type.FullName).Join($"{Environment.NewLine}    ")}");
            }
        }
    }
}
