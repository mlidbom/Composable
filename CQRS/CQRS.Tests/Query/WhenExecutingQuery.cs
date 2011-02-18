using System;
using System.Collections.Generic;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using Composable.CQRS;
using Composable.CQRS.Query;
using Composable.DDD;
using Microsoft.Practices.ServiceLocation;
using NUnit.Framework;

namespace CQRS.Tests.Query
{
    [TestFixture]
    public abstract class WhenExecutingQuery
    {
        private static List<ActiveCandidates.ActiveCandidate> _result = new List<ActiveCandidates.ActiveCandidate>
                                                                    {
                                                                        new ActiveCandidates.ActiveCandidate
                                                                            {
                                                                                Forename = "ForeName",
                                                                                SurName = "SurName"
                                                                            }
                                                                    };

        protected abstract IServiceLocator Locator { get; }

        [Test]
        public void RegisteredHandlerIsCalled()
        {
            Locator.GetInstance<IQueryService>().Execute(new ActiveCandidates());
            Assert.That(ActiveCandidatesHandler.ExecuteCalled, Is.True, "Execute(ModifyCandidateCommand command) should have been called");
        }

        [Test]
        public void CorrectValueIsReturned()
        {
            var result = Locator.GetInstance<IQueryService>().Execute(new ActiveCandidates());
            Assert.That(result, Is.EqualTo(_result));
        }

        [Test]
        public void ExceptionIsThrownIfThereAreDuplicateHandlers()
        {
            Assert.Throws<DuplicateHandlersException>(() => Locator.GetInstance<IQueryService>().Execute(new DuplicateHandlers()));
        }

        public class DuplicateHandlers: IQuery<DuplicateHandlers, DuplicateHandlers.ReturnType>
        {
            public class ReturnType{}
        }

        public class DuplicateHandlersHandler1 : IQueryHandler<DuplicateHandlers, DuplicateHandlers.ReturnType>
        {
            public DuplicateHandlers.ReturnType Execute(DuplicateHandlers query)
            {
                return null;
            }
        }

        public class DuplicateHandlersHandler2 : IQueryHandler<DuplicateHandlers, DuplicateHandlers.ReturnType>
        {
            public DuplicateHandlers.ReturnType Execute(DuplicateHandlers query)
            {
                return null;
            }
        }


        public class ActiveCandidatesHandler : IQueryHandler<ActiveCandidates, IEnumerable<ActiveCandidates.ActiveCandidate>>
        {
            public static bool ExecuteCalled { get; private set; }

            public IEnumerable<ActiveCandidates.ActiveCandidate> Execute(ActiveCandidates query)
            {
                ExecuteCalled = true;
                return _result;
            }
        }

        public class ActiveCandidates : IQuery<ActiveCandidates, IEnumerable<ActiveCandidates.ActiveCandidate>>
        {
            public class ActiveCandidate : ValueObject<ActiveCandidate>
            {
                public string Forename { get; set; }
                public string SurName { get; set; }
            }
        }
    }
}