using System;
using System.Diagnostics.Contracts;
using Composable.System;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventRenamedFromAttribute : Attribute
    {
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}