using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace NetMqProcess1._01_Introduction
{
    public class Server
    {
        public void Run()
        {
            using (var server = new ResponseSocket())
            {
                server.Bind("tcp://*:5555");

                while (true)
                {
                    var message = server.ReceiveFrameString();

                    Console.WriteLine("Received {0}", message);

                    // processing the request
                    Thread.Sleep(100);

                    Console.WriteLine("Sending World");
                    server.SendFrame("World");
                }
            }
        }
    }
}