using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events
{
    [TestFixture]
    public class When_mapping_names_for_classes_with_no_renaming_attribute
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
        public void Event_type_maps_to_events_full_name()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(typeof(Event1).FullName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(typeof(Event2).FullName);
        }

        [Test]
        public void Events_full_name_maps_to_event_type()
        {
            _nameMapper.GetType(typeof(Event1).FullName).Should().Be(typeof(Event1));
            _nameMapper.GetType(typeof(Event2).FullName).Should().Be(typeof(Event2));
        }

        [TypeId("4F6B80CA-37CC-4226-8E9B-332400C0187E")]class Event1 : AggregateRootEvent { }

        [TypeId("60ABB657-CF4F-4A89-90C6-2AC0C62B2E48")]class Event2 : AggregateRootEvent { }
    }
}
