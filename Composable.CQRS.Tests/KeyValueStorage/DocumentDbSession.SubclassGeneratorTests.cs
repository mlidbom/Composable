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


        [Test] public void Can_create_10_instances_per_millisecond_given_correct_windsor_wiring()
        {
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
                                        .LifestyleSingleton()
                              );

            var factoryMethod = DocumentDbSession.SubClassGenerator.CreateFactoryMethod<IInheritingInterface>();
            //warm up cache
            using (container.BeginScope())
            {
                var instance = factoryMethod(container);
            }

            TimeAsserter.Execute(() =>
                                 {
                                     using(container.BeginScope())
                                     {
                                         factoryMethod(container);
                                     }
                                 },
                                 iterations: 100,
                                 maxTotal: 10.Milliseconds());
        }
    }
}
