using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace NetMqProcess2._01_Introduction
{
    public class Client
    {
        public class Data
        {
            public long Id { get; set; }
        }

        public void Run()
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://localhost:5555");
                var sent = new List<Data>();
                var received = new List<Data>();
                for (int i = 0; i < 100000; i++)
                {
                    var data = new Data() { Id = i };
                    sent.Add(data);
                    client.SendFrame(JsonConvert.SerializeObject(data));
                    var message = client.ReceiveFrameString();
                    received.Add(JsonConvert.DeserializeObject<Data>(message));
                }

                client.SendFrame("quit");

                Console.WriteLine($@"sent.Length {sent.Count}
received.Length: {received.Count},
sent.Sum(): {sent.Select(data => data.Id).Sum()}
received.Sum(): {received.Select(data => data.Id).Sum()}
");
            }
        }
    }
}