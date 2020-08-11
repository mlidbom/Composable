using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Composable.Persistence.EventStore;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ReflectionCE.EmitCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        public static class WrapperEvent
        {
            static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();

            static class WrapperConstructorCache<TEvent> where TEvent : IEvent
            {
                static readonly Func<IEvent, IWrapperEvent<IEvent>> UntypedConstructor = CreateConstructorFor(typeof(TEvent));

                internal static readonly Func<TEvent, IWrapperEvent<TEvent>> Constructor = @event => (IWrapperEvent<TEvent>)UntypedConstructor(@event);
            }

            public static IWrapperEvent<TWrappedEvent> WrapEvent<TWrappedEvent>(TWrappedEvent theEvent) where TWrappedEvent : IEvent =>
                WrapperConstructorCache<TWrappedEvent>.Constructor(theEvent);

            public static IWrapperEvent<MessageTypes.IEvent> WrapEvent(IEvent theEvent)
            {
                var wrappedEventType = theEvent.GetType();
                if(_wrapperConstructors.TryGetValue(wrappedEventType, out var constructor)) return constructor!(theEvent);

                Monitor.Update(() =>
                {
                    constructor = CreateConstructorFor(theEvent.GetType());
                    ThreadSafe.AddToCopyAndReplace(ref _wrapperConstructors, wrappedEventType, constructor);
                });

                return constructor!(theEvent);
            }

            static Func<IEvent, IWrapperEvent<IEvent>> CreateConstructorFor(Type wrappedEventType)
            {
                var openWrapperEventType = typeof(MessageTypes.IWrapperEvent<>);
                var closedWrapperEventType = openWrapperEventType.MakeGenericType(wrappedEventType);

                var openWrapperImplementationType = CreateGenericWrapperEventType(openWrapperEventType);
                var closedWrapperImplementationType = openWrapperImplementationType.MakeGenericType(wrappedEventType);

                var constructorArgumentTypes = new[] {wrappedEventType};
                var creatorFunctionArgumentTypes = new[] {typeof(MessageTypes.IEvent)};

                var constructor = closedWrapperImplementationType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
                if(constructor == null)
                {
                    throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {closedWrapperEventType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
                }

                var constructorCallMethod = new DynamicMethod(name: $"Generated_constructor_for_{closedWrapperEventType.Name}", returnType: closedWrapperEventType, parameterTypes: creatorFunctionArgumentTypes, owner: closedWrapperImplementationType);
                var ilGenerator = constructorCallMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Castclass, wrappedEventType);
                ilGenerator.Emit(OpCodes.Newobj, constructor);
                ilGenerator.Emit(OpCodes.Ret);

                return (Func<IEvent, IWrapperEvent<IEvent>>)constructorCallMethod.CreateDelegate(typeof(Func<IEvent, IWrapperEvent<IEvent>>));
            }

            static string DescribeParameterList(IEnumerable<Type> parameterTypes) { return parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", "); }

            static IReadOnlyDictionary<Type, Func<MessageTypes.IEvent, IWrapperEvent<MessageTypes.IEvent>>> _wrapperConstructors = new Dictionary<Type, Func<IEvent, IWrapperEvent<IEvent>>>();

            static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();
            internal static Type CreateGenericWrapperEventType(Type wrapperEventType)
            {
                if(_createdWrapperTypes.TryGetValue(wrapperEventType, out var cachedWrapperImplementation))
                {
                    return cachedWrapperImplementation;
                }

                return Monitor.Update(() =>
                {
                    if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");

                    if(wrapperEventType != typeof(MessageTypes.IWrapperEvent<>)
                    && wrapperEventType.GetInterfaces().All(iface => iface != typeof(MessageTypes.IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
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

                        wrapperEventBuilder.ImplementConstructor(wrappedEventField);

                        return wrapperEventBuilder.CreateType().NotNull();
                    });

                    ThreadSafe.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

                    return genericWrapperEventType;
                });
            }
        }

        public class WrapperEvent<TEventInterface> : IWrapperEvent<TEventInterface>
            where TEventInterface : IWrapperEvent<TEventInterface>
        {
            public WrapperEvent(TEventInterface @event) => Event = @event;
            public TEventInterface Event { get; }
        }
    }
}
