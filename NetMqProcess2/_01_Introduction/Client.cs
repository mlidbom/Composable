using System;
using System.Collections.Generic;
using System.Linq;
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
                var sent = new List<long>();
                var received = new List<long>();
                for (int i = 0; i < 10000; i++)
                {
                    sent.Add(i);
                    client.SendFrame(i.ToString());
                    var message = client.ReceiveFrameString();
                    if(message == "quit")
                    {
                        //break;
                    }
                    received.Add(long.Parse(message));
                }

                client.SendFrame("quit");

                Console.WriteLine($@"sent.Length {sent.Count}
received.Length: {received.Count},
sent.Sum(): {sent.Sum()}
received.Sum(): {received.Sum()}
");
            }
        }
    }
}