using System;
using System.Collections.Generic;
using System.Threading;
using Composable.System;
using Composable.System.Collections.Collections;

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    public class Testing
    {



        public void Test()
        {
            var createAccountHandler = new TestMessageHandler<CreateAccountCommand>();
            var accountQueryModelUpdaed = new TestMessageHandler<AccountCreatedEvent>();

            var endpoint = new Endpoint(
                EventHandler.For<AccountCreatedEvent>("AD198D3E-5340-4CB3-8BDB-31AFD0C7FC9A", accountQueryModelUpdaed.Handle)
                );

            //Publish event and wait for compdes
        }


        class TestingResetEvent
        {
            private readonly ManualResetEvent _event = new ManualResetEvent(false);
            public void Wait()
            {
                if (!_event.WaitOne(10.Milliseconds()))
                {
                    throw new Exception("Timed out waiting for lock.");
                }
            }

            public void Set() => _event.Set();
        }

        class HandlerResetEvent
        {
            public TestingResetEvent Started { get; } = new TestingResetEvent();
            public TestingResetEvent Completed { get; } = new TestingResetEvent();
            public TestingResetEvent AllowToRun { get; } = new TestingResetEvent();
        }

        class HandlerResetEvents
        {
            private readonly Dictionary<string, HandlerResetEvent> _manuals = new Dictionary<string, HandlerResetEvent>();

            public HandlerResetEvent Manual(string name)
            {
                lock (_manuals)
                {
                    return _manuals.GetOrAdd(name, () => new HandlerResetEvent());
                }
            }

            public HandlerResetEvent Manual(int key) => Manual(key.ToString());
            public HandlerResetEvent Manual(Guid key) => Manual(key.ToString());
        }



        //Register a handler implemented like this and you get full insight into when it is invoked, and full control over when it is allowed to complete.
        //This should give us full testability of invokation policies :)
        class TestMessageHandler<T>
        {
            public readonly TestingResetEvent Started = new TestingResetEvent();
            public readonly  TestingResetEvent Completed = new TestingResetEvent();
            public  readonly  TestingResetEvent AllowToComplete = new TestingResetEvent();

            readonly HandlerResetEvent _events;
            public bool IsStarted = false;
            public bool IsCompleted = false;
            public bool IsRunning => IsStarted && !IsCompleted;


            public void Handle(T message)
            {
                IsCompleted = false;
                IsStarted = true;
                _events.Started.Set();
                _events.AllowToRun.Wait();
                IsCompleted = true;
                _events.Completed.Set();
            }
        }

        class TestEventHandler<T> : TestMessageHandler<T>
        {
            public TestEventHandler() 
            {
            }
        }

        class TestCommandHandler<T> : TestMessageHandler<T>
        {
            public TestCommandHandler() : base() {}
        }

        class TestHandlers
        {
            HandlerResetEvents _handlerResetEvents;
            public TestHandlers(HandlerResetEvents handlerResetEvents) => _handlerResetEvents = handlerResetEvents;

            public TestEventHandler<T> EventHandler<T>(string name, params IMessageHandlerPolicy[] policies) => new  TestEventHandler<T>();
        }
    }
}
