using System;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
// ReSharper disable All

namespace NetMqProcess01._01_Introduction
{
    public class Server
    {
        class Data
        {
            public long Id { get; set; }
        }

        public static void Run()
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
                    var data = JsonConvert.DeserializeObject<Data>(message);
                    server.SendFrame(JsonConvert.SerializeObject(data));
                    if(++messages % 1000 == 0)
                    {
                        Console.WriteLine($"Echoed  {messages} messages.");
                    }
                }
            }
        }
    }
}