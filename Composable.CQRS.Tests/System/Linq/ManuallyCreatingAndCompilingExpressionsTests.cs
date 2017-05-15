using System;
using System.Linq.Expressions;
using System.Reflection;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Linq
{
    [TestFixture]
    public class ManuallyCreatingAndCompilingExpressionsTests
    {
        [Test] public void TestName2()
        {
            using(var locator = DependencyInjectionContainer.CreateServiceLocatorForTesting(
                container =>
                {
                    container.Register(Component.For<EventHandler>().ImplementedBy<EventHandler>().LifestyleSingleton());
                }))
            {


                var eventType = typeof(IEvent);

                //Create expression to resolve the handler instance from the container
                var locatorExpression = Expression.Constant(locator);
                var resolveMethodName = ExpressionUtil.ExtractMethodName(() => ServiceLocator.Resolve<object>(null));
                var resolveInstanceMethodCallExpression = Expression.Call(typeof(ServiceLocator), resolveMethodName, new[] {typeof(EventHandler)}, locatorExpression);

                //Create expression that calls the handler method given an event parameter.
                var eventParameter = Expression.Parameter(eventType, "eventParameterName_irrelevant");
                var handleMethodName = ExpressionUtil.ExtractMethodName(() => ((IEventSubscriber<IEvent>)null).Handle(null));
                var handleMethod = typeof(IEventSubscriber<IEvent>).GetMethod(handleMethodName);
                var callHandleExpression = Expression.Call(resolveInstanceMethodCallExpression, handleMethod, eventParameter);

                //Create a lambda that takes an event parameter and calls the candler method with it.
                var eventHandlerLambda = Expression.Lambda(callHandleExpression, eventParameter);



                var eventHandlerMethod = eventHandlerLambda.Compile();
                var action = (Action<IEvent>)eventHandlerMethod;

                //create expression to register the event handler
                var registrar = locator.Resolve<IMessageHandlerRegistrar>();
                var registrarExpression = Expression.Constant(registrar, typeof(IMessageHandlerRegistrar));
                var forEventMethodName = ExpressionUtil.ExtractMethodName<IMessageHandlerRegistrar>(() => registrar.ForEvent<IEvent>(null));
                var forEventMethodCallExpression = Expression.Call(registrarExpression, forEventMethodName, new[] {eventType}, eventHandlerLambda);

                var registerLambda = Expression.Lambda(forEventMethodCallExpression);
                var registerMethod = (Func<IMessageHandlerRegistrar>)registerLambda.Compile();

                registerMethod();

                Console.WriteLine(forEventMethodCallExpression);

                locator.Resolve<IInProcessServiceBus>().Publish(new Event());
            }

        }

        class Event : IEvent
        {
            public override string ToString() => "SomeEvent";
        }

        class EventHandler : IEventSubscriber<IEvent>
        {
            public void Handle(IEvent message)
            {
                Console.WriteLine($"Handle called with argument:{message}");
            }
        }

        [Test] public void TestName()
        {
            var lambda = GetExpressionForBarEqualsArgumentValue("abc");
            Foo foo = new Foo { Bar = "aabca" };
            bool test = lambda.Compile()(foo);

            test.Should()
                .Be(true);
        }

        static Expression<Func<Foo,bool>> GetExpressionForBarEqualsArgumentValue(string propertyValue)
        {
            //t => t.SomeProperty.Contains("stringValue");
            var fooTypeParameter = Expression.Parameter(typeof(Foo), "nameOfParameterInGeneratedLambda");
            var barPropertyExpression = Expression.Property(fooTypeParameter, "Bar");
            var stringContainsMethodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var propertyValueConstantExpression = Expression.Constant(propertyValue, typeof(string));
            var containsMethodCallExpression = Expression.Call(barPropertyExpression, stringContainsMethodInfo, propertyValueConstantExpression);

            Console.WriteLine(containsMethodCallExpression);

            var lambdaExpression = Expression.Lambda(containsMethodCallExpression, fooTypeParameter);

            Console.WriteLine(lambdaExpression);

            return (Expression<Func<Foo, bool>>)lambdaExpression;
        }

        class Foo
        {
            public string Bar { get; set; }
        }
    }
}
