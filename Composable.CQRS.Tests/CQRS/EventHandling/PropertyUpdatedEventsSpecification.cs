using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.KeyValueStorage;
using Composable.CQRS.Tests.CQRS.EventHandling.CVManagement;
using Composable.CQRS.Tests.CQRS.EventHandling.CVManagement.GlobalEvents;
using Composable.CQRS.Tests.CQRS.EventHandling.CVManagement.InternalEvents.InternalImplementations;
using Composable.CQRS.Tests.CQRS.EventHandling.CVManagement.QueryModelUpdaters;
using Composable.DDD;
using Composable.Messaging.Events;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable UnusedMember.Global

namespace Composable.CQRS.Tests.CQRS.EventHandling
{
    namespace CVManagement
    {
        namespace GlobalEvents
        {

            #region Generic events intended to be inherited. None of these should ever be raised. Only inheritors should be raised.

            //Every single CV event should inherit this one. Directly or indirectly.
            public interface ICvEvent : IAggregateRootEvent {}

            //Note how this event inherits IAggregateRootCreatedEvent. This allows SingleAggregateQueryModelUpdater and others to automatically know that it is a creation event.
            //Any event that signifies that a CV has been created should inherit this event
            //That way SingleAggregateQueryModelUpdater will not try to read the model from the DB and it can automatically instantiate the model with the correct aggregate root id.
            interface ICvCreated : ICvEvent, IAggregateRootCreatedEvent {}

            //Note how this event inherits IAggregateRootDeletedEvent. This allows SingleAggregateQueryModelUpdater and others to automatically know that it is a deletion event.
            //That way SingleAggregateQueryModelUpdater will know that it can go right ahead and delete the model.
            public interface ICvDeleted : ICvEvent, IAggregateRootDeletedEvent {}

            //Should be inherited by any event that is triggered by the candidate editing his/her CV
            interface ICvUpdatedByOwner : ICvEvent {}

            //Should be inherited by any event that is triggered by a recruiter acting on the CV
            public interface ICvUpdatedByRecruiter : ICvEvent
            {
                Guid RecruiterId { get; set; }
            }

            //Property updated events. There should be one per property or collection.
            //All other events MUST inherit each PropertyUpdated event that applies. They should NOT add properties that are part of the aggregate in any way but by inheriting
            //a PropertyUpdated event.
            namespace PropertyUpdated
            {
                interface ICvEmailPropertyUpdated : ICvEvent
                {
                    string Email { get; set; }
                }

                interface ICvPasswordPropertyUpdated : ICvEvent
                {
                    string Password { get; set; }
                }

                //For collections PropertyUpdated events should handle List<T>. Not Items.
                //They should have both Added* and Removed* properties.
                public interface ICvSkillsPropertyUpdated : ICvEvent
                {
                    List<string> AddedSkills { get; set; }
                    List<string> RemovedSkills { get; set; }
                }
            }

            #endregion

            interface ICvRegistered : ICvCreated, PropertyUpdated.ICvEmailPropertyUpdated, PropertyUpdated.ICvPasswordPropertyUpdated {}

            interface ICvSkillsEditedByCandidate : PropertyUpdated.ICvSkillsPropertyUpdated, ICvUpdatedByOwner {}

            public interface ICvSkillsEditedByRecruiter : PropertyUpdated.ICvSkillsPropertyUpdated, ICvUpdatedByRecruiter {}
        }

        public class CvQueryModel : ValueObject<CvQueryModel>, ISingleAggregateQueryModel
        {
            public Guid Id { get; private set; }
            public void SetId(Guid id)
            {
                Id = id;
            }

            internal string Email { get; set; }
            internal string Password { get; set; }
            internal HashSet<string> Skills { get; }

            public CvQueryModel()
            {
                Skills = new HashSet<string>();
            }
        }

        namespace InternalEvents
        {
            namespace InternalImplementations
            {
                class CvRegisteredEvent : AggregateRootEvent, ICvRegistered
                {
                    public string Email { get; set; }
                    public string Password { get; set; }
                }

                class CvSkillsEdited : AggregateRootEvent, ICvSkillsEditedByCandidate
                {
                    public List<string> AddedSkills { get; set; }
                    public List<string> RemovedSkills { get; set; }
                }
            }
        }

        namespace QueryModelUpdaters
        {
            class CvQueryModelUpdater : SingleAggregateQueryModelUpdater<CvQueryModelUpdater, CvQueryModel, ICvEvent, IDocumentDbSession>
            {
                public CvQueryModelUpdater(IDocumentDbSession session)
                    : base(session)
                {
                    RegisterHandlers()
                        .For<GlobalEvents.PropertyUpdated.ICvEmailPropertyUpdated>(e => Model.Email = e.Email)
                        .For<GlobalEvents.PropertyUpdated.ICvPasswordPropertyUpdated>(e => Model.Password = e.Password)
                        .For<GlobalEvents.PropertyUpdated.ICvSkillsPropertyUpdated>(e =>
                                                                                    {
                                                                                        Model.Skills.RemoveRange(e.RemovedSkills);
                                                                                        Model.Skills.AddRange(e.AddedSkills);
                                                                                    });
                }
            }
        }
    }

    public class PropertyUpdatedEventsSpecification : nspec
    {
        public void starting_from_empty()
        {
            CvQueryModel cvQueryModel = null;
            CvQueryModelUpdater cvQueryModelUpdater = null;
            IDocumentDbSession documentDbSession = null;
            Guid cvId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            before = () =>
                     {
                         cvQueryModel = null;
                         documentDbSession = new DocumentDbSession(new InMemoryDocumentDb(), new SingleThreadUseGuard(), NullOpDocumentDbSessionInterceptor.Instance);
                         cvQueryModelUpdater = new CvQueryModelUpdater(documentDbSession);
                     };

            context["after receiving CVRegisteredAvent"] =
                () =>
                {
                    CvRegisteredEvent registeredEvent = null;
                    before = () =>
                             {
                                 registeredEvent = new CvRegisteredEvent()
                                                   {
                                                       AggregateRootId = cvId,
                                                       Email = "Email",
                                                       Password = "Password"
                                                   };
                                 cvQueryModelUpdater.Handle(registeredEvent);
                                 cvQueryModel = documentDbSession.Get<CvQueryModel>(cvId);
                             };
                    it["does not crash :)"] = () => Assert.True(true);
                    it["_resultingModel.Id is template.id"] = () => cvQueryModel.Id.Should().Be(registeredEvent.AggregateRootId);
                    it["_resultingModel.Email is template.Email"] = () => cvQueryModel.Email.Should().Be(registeredEvent.Email);
                    it["_resultingModel.Password is template.Password"] = () => cvQueryModel.Password.Should().Be(registeredEvent.Password);
                    context["after receiving CVSkillsEditedEvent"] =
                        () =>
                        {
                            CvSkillsEdited skillsEdited = null;
                            before = () =>
                                     {
                                         skillsEdited = new CvSkillsEdited()
                                                        {
                                                            AggregateRootId = registeredEvent.AggregateRootId,
                                                            AddedSkills = new List<string> {"AddedSkill1", "AddedSkill2", "AddedSkill3"},
                                                            RemovedSkills = new List<string> {"RemovedSkill1"}
                                                        };
                                         cvQueryModelUpdater.Handle(skillsEdited);
                                     };

                            it["CVQueryModel.Skills is event.AddedSkills "] = () => cvQueryModel.Skills.Should().Equal(skillsEdited.AddedSkills);
                        };
                };
        }
    }
}
