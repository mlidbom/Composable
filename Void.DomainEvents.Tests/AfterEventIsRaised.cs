using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Composable.System.Linq;
using Composable.System;

namespace Composable.DomainEvents.Tests
{        
    [TestFixture]
    public class AfterEventIsRaised
    {
        public class SomethingHappend : IDomainEvent
        { }

        [Test]
        public void HandlersAreNotReused()
        {
            ISet<DisposableHandler> usedHandlers = new HashSet<DisposableHandler>();
            Action<DisposableHandler> addToHandlers = handler => usedHandlers.Add(handler);
            DisposableHandler.HandledEvent += addToHandlers;

            try
            {

                10.Times(() => DomainEvent.Raise(new SomethingHappend()));
                Assert.That(usedHandlers.Count, Is.EqualTo(10));
            }finally
            {
                DisposableHandler.HandledEvent -= addToHandlers;
            }
        }

        [Test]
        public void HandlersImplementingIDisposableAreDisposed()
        {
            10.Times(() => DomainEvent.Raise(new SomethingHappend()));
            Assert.That(DisposableHandler.Instances, Is.EqualTo(0));
        }


        public class DisposableHandler : IHandles<SomethingHappend>, IDisposable
        {
            public static int Instances { get; private set; }

            public DisposableHandler()
            {
                ++Instances;
            }

            public void Dispose()
            {
                Instances--;
            }

            ~DisposableHandler()
            {
                Dispose();
            }

            public void Handle(SomethingHappend happening)
            {
                HandledEvent(this);
            }

            public static event Action<DisposableHandler> HandledEvent = _ => { };
        }
    }
}