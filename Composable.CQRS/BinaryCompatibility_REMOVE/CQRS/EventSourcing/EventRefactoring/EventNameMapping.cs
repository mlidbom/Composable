using System;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    public class EventNameMapping
    {
        private string _fullName;
        public EventNameMapping(Type type)
        {
            Type = type;
            FullName = type.FullName;
        }

        public Type Type { get; }

        public string FullName
        {
            get
            {
                return _fullName;
            }
            set
            {
                Type ignored;
                if (value != Type.FullName && value.TryGetType(out ignored))
                {
                    throw new Exception($"Attempted to rename event type { Type.FullName } to { value }, but there is already a class with that FullName");
                }
                _fullName = value;
            }
        }
    }
}