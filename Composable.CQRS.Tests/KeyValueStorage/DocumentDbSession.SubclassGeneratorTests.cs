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
    [TestFixture] public class DocumentDbSession_SubclassGeneratorTests
    {
        //Make the interface nested to ensure that we support that case. Just about every other interface used will not be so I do not test specifically for that here.
        // ReSharper disable once MemberCanBePrivate.Global
        public interface IInheritingInterface : IDocumentDbSession {}


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

            var factoryMethod = RuntimeInstanceGenerator.DocumentDbSession.CreateFactoryMethod<IInheritingInterface>();
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
