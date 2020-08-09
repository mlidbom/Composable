using Composable.Messaging;
using Composable.Persistence.EventStore;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Composable.SystemCE.ReflectionCE.EmitCE;

namespace ScratchPad.ReflectionEmit
{
    public interface IUserWrapperEvent<out TWrappedUserEvent> : MessageTypes.IWrapperEvent<TWrappedUserEvent>
        where TWrappedUserEvent : IUserEvent {}

    public interface IUserEvent : MessageTypes.IEvent {}

    class UserEvent : IUserEvent {}

    public class Example
    {
        [Test] public void BuildWrapperEventType()
        {
            var genericWrapperEventType = CreateGenericWrapperEventType(typeof(IUserWrapperEvent<>));

            //instantiate a concrete version.
            Type wrapperEventIUserEvent = genericWrapperEventType.MakeGenericType(typeof(IUserEvent));

            var instance = (IUserWrapperEvent<IUserEvent>)Activator.CreateInstance(wrapperEventIUserEvent).NotNull();

            var wrappedProperty = wrapperEventIUserEvent.GetProperty(nameof(MessageTypes.IWrapperEvent<IAggregateEvent>.Event)).NotNull();

            var userEvent = new UserEvent();
            instance.Event.Should().Be(null);
            wrappedProperty.SetValue(instance, userEvent);
            instance.Event.Should().Be(userEvent);
        }

        static Type CreateGenericWrapperEventType(Type wrapperEventType)
        {
            if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");
            if(wrapperEventType.GetInterfaces().All(iface => iface != typeof(MessageTypes.IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
                throw new ArgumentException($"Must implement {typeof(MessageTypes.IWrapperEvent<>).FullName}", $"{nameof(wrapperEventType)}");

            var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

            var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(MessageTypes.IEvent).IsAssignableFrom(constraint));

            var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
            {
                TypeBuilder wrapperEventBuilder = module.DefineType(
                    name: $"{wrapperEventType}_ilgen_impl",
                    attr: TypeAttributes.Public,
                    parent: null,
                    interfaces: new[] {wrapperEventType});

                GenericTypeParameterBuilder wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

                wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

                var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(MessageTypes.IWrapperEvent<IAggregateEvent>.Event), wrappedEventTypeParameter);

                return wrapperEventBuilder.CreateType().NotNull();
            });
            return genericWrapperEventType;
        }
    }
}
