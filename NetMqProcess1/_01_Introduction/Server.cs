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

                int messages = 0;
                while (true)
                {
                    var message = server.ReceiveFrameString();
                    if(message == "quit")
                    {
                        break;
                    }
                    server.SendFrame(message);
                    if(++messages % 1000 == 0)
                    {
                        Console.WriteLine($"Echoed  {messages} messages.");
                    }
                }
            }
        }
    }
}