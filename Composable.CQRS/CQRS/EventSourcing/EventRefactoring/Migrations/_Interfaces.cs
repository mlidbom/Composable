using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventModifier
    {
        void Replace(IEnumerable<IAggregateRootEvent> events);
        void InsertBefore(IEnumerable<IAggregateRootEvent> events);
        //void InsertAfter(IEnumerable<IAggregateRootEvent> events); //Will not support guaranteeing that the migration is stable(Does not recursively change the stream again and again.) and will therefore not be supported
        void Ignore();
    }

    public interface IReplaceMyself<TEvent>
    where TEvent : IAggregateRootEvent, IReplaceMyself<TEvent>
    {
        IEnumerable<IAggregateRootEvent> ReplaceYourSelf();
    }
}