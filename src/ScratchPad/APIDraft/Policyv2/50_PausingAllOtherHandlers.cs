﻿// ReSharper disable All
#pragma warning disable //Review OK: This is API experimental code that is never ever used.

namespace Composable.Tests.Messaging.APIDraft.Policyv2
{
    public class PausingAllOtherHandlers
    {
        void IllustratateRegistration()
        {

            var pauseAllOtherHandlers = new CompositePolicy(
                Policy.LockExclusively.CommandProcessing,
                Policy.LockExclusively.EventProcessing
            );

            var policiesAsInterfaces = new Endpoint(
                //Command handlers
                //various normal command and event handler registrations

                CommandHandler.For<OptimizeEventStoreCommand>("F9688A3B-F6AF-4884-9FB5-F6670718F6BE", command => { }, pauseAllOtherHandlers),
                CommandHandler.For<OptimizeDocumentDbCommand>("7A2DC4C3-F2DB-43BD-ACB0-BF454BC6C958", command => { }, pauseAllOtherHandlers)
            );
        }

    }
    // ReSharper disable once UnusedMember.Global

    class OptimizeDocumentDbCommand {}

    class OptimizeEventStoreCommand {}
}
