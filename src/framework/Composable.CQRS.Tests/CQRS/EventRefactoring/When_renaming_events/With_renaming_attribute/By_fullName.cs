using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events.With_renaming_attribute
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

        [TypeId("9A10A06D-776F-4652-9476-6F94AB612840")][EventRenamedFrom(FullName = OldName)] class Event1 : AggregateRootEvent
        {
            public const string OldName= "Even1OldName";
        }

        [TypeId("361E64D9-05A0-46B2-8FEA-786C8C10003B")][EventRenamedFrom(FullName = OldName)] class Event2 : AggregateRootEvent
        {
            public const string OldName = "Event2OldName";
        }
    }
}
