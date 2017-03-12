using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.KeyValueStorage;
using Composable.Persistence.KeyValueStorage;
using Composable.System;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    // ReSharper disable once MemberCanBeInternal
    public interface IInheritingInterface : IDocumentDbSession {}

    [TestFixture] public class DocumentDbSession_SubclassGeneratorTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public interface INestedInheritingInterface : IDocumentDbSession {}

        [Test] public void CanCreateSubclass()
        {
            var inheritingType = DocumentDbSession.SubClassGenerator.GenerateSubClass<IInheritingInterface>();
        }

        [Test] public void CanCreateSubclassForNestedInterface()
        {
            var inheritingType = DocumentDbSession.SubClassGenerator.GenerateSubClass<INestedInheritingInterface>();
        }

        [Test] public void Can_create_10_instances_per_millisecond_given_correct_windsor_wiring()
        {
            var sessionType = DocumentDbSession.SubClassGenerator.GenerateSubClass<IInheritingInterface>();

            var container = new WindsorContainer();
            container.Register(
                               Component.For<IDocumentDb>()
                                        .ImplementedBy<InMemoryDocumentDb>()
                                        .LifestyleSingleton(),
                               Component.For<ISingleContextUseGuard>()
                                        .ImplementedBy<SingleThreadUseGuard>()
                                        .LifestyleScoped(),
                               Component.For<IDocumentDbSessionInterceptor>()
                                        .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                                        .LifestyleSingleton(),
                               Component.For<IInheritingInterface>()
                                        .ImplementedBy(sessionType)
                                        .LifestyleScoped()
                              );

            using(container.BeginScope())
            {
                container.Resolve<IInheritingInterface>();
            }
        }
    }
}
