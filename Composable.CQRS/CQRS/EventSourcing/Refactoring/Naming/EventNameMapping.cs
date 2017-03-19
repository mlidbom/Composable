using System;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    class EventNameMapping
    {
        string _fullName;
        public EventNameMapping(Type type)
        {
            Type = type;
            FullName = type.FullName;
        }

        public Type Type { get; }

        public string FullName
        {
            get => _fullName;
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