using System;
using System.Linq;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    public class RenameEventsBasedOnEventRenamedAttributes : IRenameEvents
    {
        public void Rename(EventNameMapping mapping)
        {
            var renamingAttribute =
                (EventRenamedFromAttribute)mapping.Type.GetCustomAttributes(typeof(EventRenamedFromAttribute), inherit: false).SingleOrDefault();
            if(renamingAttribute != null)
            {
                mapping.FullName = CreateFullName(mapping, renamingAttribute);
            }
        }

        string CreateFullName(EventNameMapping mapping, EventRenamedFromAttribute renaming)
        {
            if(renaming.FullName != null)
            {
                if(renaming.Name != null || renaming.Path != null)
                {
                    throw new Exception(
                        $"You cannot pass FullName together with Name or Path to an {nameof(EventRenamedFromAttribute)}.  Invalid attribute applied to type: {mapping}");
                }
                return renaming.FullName;
            }

            if(renaming.Path == null)
            {
                if(renaming.Name == null)
                {
                    throw new Exception(
                        $"You must specify at least one out of {nameof(renaming.Path)}, {nameof(renaming.Name)} and {nameof(renaming.FullName)} in an {nameof(EventRenamedFromAttribute)}. Invalid attribute applied to type: {mapping}");
                }

                if(!mapping.FullName.EndsWith($".{mapping.Type.Name}") && !mapping.FullName.EndsWith($"+{mapping.Type.Name}"))
                {
                    throw new Exception($@"Attempting to rename event class based on {nameof(EventRenamedFromAttribute)} but the new class name has already been removed from the mapped full name. 
current mapping FullName: {mapping.FullName}
Attribute applied to type: { mapping.Type }");

                }
            }

            var path = renaming.Path ?? mapping.FullName.Substring(0, mapping.FullName.Length - mapping.Type.Name.Length);
            var name = renaming.Name ?? mapping.Type.Name;

            return path + name;
        }
    }
}
