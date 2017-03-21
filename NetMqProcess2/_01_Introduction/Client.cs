using System.Collections.Generic;
using System.Linq;
using Composable.Logging;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace NetMqProcess02._01_Introduction
{
    static class Client
    {
        class Data
        {
            public long Id { get; set; }
        }

        public static void Run()
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

                SafeConsole.WriteLine($@"sent.Length {sent.Count}
received.Length: {received.Count},
sent.Sum(): {sent.Select(data => data.Id).Sum()}
received.Sum(): {received.Select(data => data.Id).Sum()}
");
            }
        }
    }
}