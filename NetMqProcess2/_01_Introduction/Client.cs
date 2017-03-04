using System;
using NetMQ;
using NetMQ.Sockets;

namespace NetMqProcess2._01_Introduction
{
    public class Client
    {
        public void Run()
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://localhost:5555");

                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine("Sending Hello");
                    client.SendFrame("Hello");

                    var message = client.ReceiveFrameString();
                    Console.WriteLine("Received {0}", message);
                }
            }
        }
    }
}