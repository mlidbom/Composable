using System;
using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.Messaging.NetMQ
{
    [TestFixture] public class API_exploration_tests
    {
        [Test] public void Play_with_router()
        {
            using var router = new RouterSocket();
            using var dealer = new DealerSocket();
            var dealerId = new Byte[] { 5 };
            dealer.Options.Identity = dealerId;

            var address = router.BindLocalhostPort();
            dealer.Connect(address);
            Console.WriteLine(address);

            dealer.SendMoreFrame(Array.Empty<byte>());
            dealer.SendFrame("Hello!");

            Console.WriteLine($"#1# {router.ReceiveFrameString()}");
            Console.WriteLine($"#2# {router.ReceiveFrameString()}");
            Console.WriteLine($"#2# {router.ReceiveFrameString()}");
        }
    }

    static class SocketExtensions
    {
        public static string BindLocalhostPort(this NetMQSocket @this) => $"tcp://localhost:{@this.BindRandomPort("tcp://localhost")}";
    }
}
