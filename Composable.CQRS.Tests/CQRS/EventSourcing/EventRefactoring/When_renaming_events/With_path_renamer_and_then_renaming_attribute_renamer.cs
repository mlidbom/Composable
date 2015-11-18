using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.When_renaming_events
{
    public class With_path_renamer_and_then_renaming_attribute_renamer
    {
        private const string OldPath = "Some.Old.Path.";

        private RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                Seq.OfTypes<Event1, Event2>(),
                new EventPathRenamer(
                    oldPath: OldPath,
                    eventAtNewPath: typeof(Event1)
                    ),
                new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldName);
        }

        [Test]
        public void Event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldName).Should().Be(typeof(Event2));
        }

        [EventRenamedFrom(Name = "OldEvent1")]
        private class Event1 : AggregateRootEvent
        {
            public const string OldName = OldPath + "OldEvent1";
        }

        [EventRenamedFrom(Name = "OldEvent2")]
        private class Event2 : AggregateRootEvent
        {
            public const string OldName = OldPath + "OldEvent2";
        }
    }
}