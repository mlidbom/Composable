using System;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    public interface IRenameEvents
    {
        void Rename(EventNameMapping mapping);
    }
}