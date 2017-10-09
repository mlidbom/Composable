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
            using(var router = new RouterSocket())
            using(var dealer = new DealerSocket())
            {
                var dealerId = new Byte[] { 5 };
                dealer.Options.Identity = dealerId;

                var port = router.BindLocalhostPort();
                dealer.ConnectLocalhostPort(port);
                Console.WriteLine(port);

                dealer.SendMoreFrame(new byte[0]);
                dealer.SendFrame("Hello!");

                Console.WriteLine($"#1# {router.ReceiveFrameString()}");
                Console.WriteLine($"#2# {router.ReceiveFrameString()}");
                Console.WriteLine($"#2# {router.ReceiveFrameString()}");
            }
        }
    }

    static class SocketExtensions
    {
        public static int BindLocalhostPort(this NetMQSocket @this) => @this.BindRandomPort("tcp://localhost");
        public static void ConnectLocalhostPort(this NetMQSocket @this, int port) => @this.Connect($"tcp://localhost:{port}");
    }
}
