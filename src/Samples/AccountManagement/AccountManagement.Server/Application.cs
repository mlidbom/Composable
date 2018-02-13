using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
// ReSharper disable LocalizableElement

namespace AccountManagement
{
    public class Application
    {
        public static void Main()
        {
            var host = EndpointHost.Production.Create(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(host);
            host.Start();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
