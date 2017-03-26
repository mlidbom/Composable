using System;
using Composable.DependencyInjection;
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
            var container = DependencyInjectionContainer.Create();
            var serviceLocator = container.CreateServiceLocator();
            var unitOfWorkSpy = new UnitOfWorkSpy();
            container.Register(
                CComponent.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>().LifestyleSingleton(),
                CComponent.For<IUnitOfWorkParticipant>().Instance(unitOfWorkSpy).LifestyleSingleton()
                );

            using(serviceLocator.BeginTransactionalUnitOfWorkScope())
            {
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
                using (var innerScope = serviceLocator.BeginTransactionalUnitOfWorkScope())
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
            var container = DependencyInjectionContainer.Create();
            var serviceLocator = container.CreateServiceLocator();
            var unitOfWorkSpy = new UnitOfWorkSpy();
            container.Register(
                CComponent.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>().LifestyleSingleton(),
                CComponent.For<IUnitOfWorkParticipant>().Instance(unitOfWorkSpy).LifestyleSingleton()
                );

            using(var outerScope = serviceLocator.BeginTransactionalUnitOfWorkScope())
            {
                unitOfWorkSpy.UnitOfWork.Should().NotBe(null);
                unitOfWorkSpy.Committed.Should().Be(false);
                unitOfWorkSpy.RolledBack.Should().Be(false);
                using (var innerScope = serviceLocator.BeginTransactionalUnitOfWorkScope())
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
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public Guid Id { get;  }

        public void Join(IUnitOfWork unit)
        {
            UnitOfWork = unit;
        }

        public void Commit(IUnitOfWork unit)
        {
            Committed = true;
            UnitOfWork = null;
        }
        public bool Committed { get; private set; }

        public void Rollback(IUnitOfWork unit)
        {
            RolledBack = true;
            UnitOfWork = null;
        }
        public bool RolledBack { get; private set; }
    }
}