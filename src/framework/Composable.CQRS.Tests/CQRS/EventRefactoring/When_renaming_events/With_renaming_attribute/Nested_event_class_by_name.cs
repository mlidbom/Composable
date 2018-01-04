using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events.With_renaming_attribute
{
    [TestFixture]
    public class Nested_event_class_by_name
    {
        RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                eventTypes: Seq.OfTypes<Event1, Event2>(),
                renamers: new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_name_replaced_but_path_retained()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldFullName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldFullName);
        }

        [Test]
        public void Event_name_with_name_replaced_but_path_retained_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldFullName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldFullName).Should().Be(typeof(Event2));
        }

        [EventRenamedFrom(Name = OldName)] [TypeId("25E3E334-81F0-4567-AD91-28705E401AEC")]class Event1 : AggregateRootEvent
        {
            const string OldName = "Event1Old";
            public static readonly string OldFullName = typeof(Event1OldRemove).FullName.Replace("Remove", "");
        }

        [EventRenamedFrom(Name = OldName)] [TypeId("00C05682-46DE-45D3-BF5D-6ED396906E35")]class Event2 : AggregateRootEvent
        {
            const string OldName = "Event2Old";
            public static readonly string OldFullName = typeof(Event2OldRemove).FullName.Replace("Remove", "");
        }

        [TypeId("6276D894-FAEC-4B89-80E1-33DB56B11ABC")]class Event1OldRemove : AggregateRootEvent { }
        [TypeId("E2736A05-D2FB-42EA-8B18-3C311F6AC9D4")]class Event2OldRemove : AggregateRootEvent { }
    }
}
