using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring;
using Composable.CQRS.EventSourcing.EventRefactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.When_renaming_events.With_renaming_attribute
{
    [TestFixture]
    public class By_path
    {
        private RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                eventTypes: Seq.OfTypes<Event1, Event2>(),
                renamers: new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_namespace_replaced_but_class_name_retained()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldName);
        }

        [Test]
        public void Event_name_with_namespace_replaced_but_class_name_retained_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldName).Should().Be(typeof(Event2));
        }

        [EventRenamedFrom(Path = OldPath)]
        private class Event1 : AggregateRootEvent
        {
            public const string OldPath= "Even1OldNamespace.";
            public const string OldName = OldPath + nameof(Event1);
        }

        [EventRenamedFrom(Path = OldPath)]
        private class Event2 : AggregateRootEvent
        {
            public const string OldPath = "Event2OldNamespace.";
            public const string OldName = OldPath + nameof(Event2);
        }
    }   
}
