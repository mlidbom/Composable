using System;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventRenamedFromAttribute : Attribute
    {
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}