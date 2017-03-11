using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.UnitOfWorkTests
{
    [TestFixture]
    public class WindsorUnitOfWorkExtensionsTests
    {
        [Test]
        public void CommitInNestedScopeDoesNothing()
        {
            var container = new WindsorContainer();
            var unitOfWorkSpy = new UnitOfWorkSpy();
            container.Register(
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<IUnitOfWorkParticipant>().Instance(unitOfWorkSpy)
                );

            using(var outerScope = container.BeginTransactionalUnitOfWorkScope())
            {
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
                using (var innerScope = container.BeginTransactionalUnitOfWorkScope())
                {
                    innerScope.Commit();
                    unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                    unitOfWorkSpy.Committed.Should().Be(false);
                    unitOfWorkSpy.RolledBack.Should().Be(false);
                }
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
            }
            unitOfWorkSpy.UnitOfWork.Should().Be(null);
            unitOfWorkSpy.Committed.Should().Be(false);
            unitOfWorkSpy.RolledBack.Should().Be(true);
        }

        [Test]
        public void CommittingTheOuterScopeCommitsDuh()
        {
            var container = new WindsorContainer();
            var unitOfWorkSpy = new UnitOfWorkSpy();
            container.Register(
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<IUnitOfWorkParticipant>().Instance(unitOfWorkSpy)
                );

            using(var outerScope = container.BeginTransactionalUnitOfWorkScope())
            {
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
                using (var innerScope = container.BeginTransactionalUnitOfWorkScope())
                {
                    innerScope.Commit();
                    unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                    unitOfWorkSpy.Committed.Should().Be(false);
                    unitOfWorkSpy.RolledBack.Should().Be(false);
                }
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
                outerScope.Commit();
            }
            unitOfWorkSpy.UnitOfWork.Should().Be(null);
            unitOfWorkSpy.Committed.Should().Be(true);
            unitOfWorkSpy.RolledBack.Should().Be(false);
        }
    }

    class UnitOfWorkSpy : IUnitOfWorkParticipant
    {
        public IUnitOfWork UnitOfWork { get; private set; }
        public Guid Id { get; private set; }

        public void Join(IUnitOfWork unit)
        {
            UnitOfWork = unit;
        }

        public void Commit(IUnitOfWork unit)
        {
            Committed = true;
            UnitOfWork = null;
        }
        public bool Committed { get; set; }

        public void Rollback(IUnitOfWork unit)
        {
            RolledBack = true;
            UnitOfWork = null;
        }
        public bool RolledBack { get; set; }
    }
}