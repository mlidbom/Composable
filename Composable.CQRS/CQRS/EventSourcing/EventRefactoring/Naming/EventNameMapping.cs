using System;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Naming
{
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