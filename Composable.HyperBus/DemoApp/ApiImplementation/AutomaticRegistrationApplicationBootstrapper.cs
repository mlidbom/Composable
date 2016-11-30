using System;
using System.Diagnostics;
using Composable.HyperBus.APIDraft;

namespace Composable.HyperBus.DemoApp.ApiImplementation
{
    public static class AutomaticRegistrationApplicationBootstrapper
    {
        public static void RegisterMessageHandlersAutomatically(IMessageHandlerRegistrar registerMessageHandlers)
        {
            registerMessageHandlers.FromAssemblyContaining(typeof(ManualRegistrationApplicationBootstrapper));
        }
    }
}