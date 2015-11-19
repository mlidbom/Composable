using System;
using System.Diagnostics.Contracts;
using Composable.System;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    [AttributeUsage(AttributeTargets.Class)]
    public class EventRenamedFromAttribute : Attribute
    {
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}