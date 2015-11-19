using System;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    public class EventPathRenamer : IRenameEvents
    {
        private string OldPath { get; }
        private string NewPath { get;  }

        public EventPathRenamer(string oldPath, Type eventAtNewPath)
        {
            OldPath = oldPath;
            NewPath = eventAtNewPath.FullName.Substring(0 , eventAtNewPath.FullName.Length - eventAtNewPath.Name.Length);
        }

        public void Rename(EventNameMapping mapping)
        {
            if(mapping.FullName.StartsWith(NewPath))
            {
                mapping.FullName = OldPath + mapping.FullName.Substring(NewPath.Length);
            }
        }
    }
}