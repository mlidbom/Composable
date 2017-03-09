using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.When_renaming_events.With_renaming_attribute
{
    [TestFixture]
    public class By_fullName
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
        public void Event_type_maps_to_event_attribute_configured_FullName()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldName);
        }

        [Test]
        public void Attribute_configured_FullName_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldName).Should().Be(typeof(Event2));
        }

        [EventRenamedFrom(FullName = OldName)] class Event1 : AggregateRootEvent
        {
            public const string OldName= "Even1OldName";
        }

        [EventRenamedFrom(FullName = OldName)] class Event2 : AggregateRootEvent
        {
            public const string OldName = "Event2OldName";
        }
    }
}
