using System;
using System.Threading;
using System.Threading.Tasks;
using Composable.Logging;
using Composable.System.Linq;
using NetMQ;
using NetMQ.Sockets;

namespace NetMqProcess01.__Mine
{
    // ReSharper disable once InconsistentNaming
    static class _01_ReqRouterDealerRep
    {
        static readonly ILogger Log = Logger.For(typeof(_01_ReqRouterDealerRep));
        const bool UseInProcess = true;

        // ReSharper disable UnreachableCode
        static string _routerSocket = UseInProcess ? "inproc://router-socket" : "tcp://127.0.0.1:5559";
        static string _dealerSocket = UseInProcess ? "inproc://dealer-socket" : "tcp://127.0.0.1:5560";
        // ReSharper restore UnreachableCode
        static readonly int NumberOfServers = 100;
        static readonly int NumberOfClient = 10;

        public static void Run()
        {
            using(var serverPoller = new NetMQPoller())
            {
                StartBroker();

                Thread.Sleep(1000);
                StartServers(serverPoller);
                StartClients();

                serverPoller.RunAsync();
                Console.ReadLine();
            }
        }

        static void StartServers(NetMQPoller serverPoller)
        {
            var threadServerSocket = new ThreadLocal<ResponseSocket>(trackAllValues:true);
            1.Through(NumberOfServers)
             .ForEach(serverId =>
                      {
                          Task.Run(() =>
                                   {
                                       ResponseSocket responseSocket;

                                       void HandleRequest(object sender, NetMQSocketEventArgs socketEventArgs)
                                       {
                                           NetMQMessage request = null;
                                           // ReSharper disable once AccessToModifiedClosure
                                           if(responseSocket.TryReceiveMultipartMessage(ref request))
                                           {
                                               var clientId = request.First.ConvertToInt32();

                                               //SafeConsole.WriteLine($"Server {serverId} got request from client: {clientId}");
                                               var response = new NetMQMessage(new[]
                                                                               {
                                                                                   new NetMQFrame($"Server:{serverId}, Client:{clientId}")
                                                                               });

                                               //SafeConsole.WriteLine($"Server {serverId} responding to client: {clientId}");
                                               // ReSharper disable once AccessToModifiedClosure
                                               responseSocket.SendMultipartMessage(response);
                                           }
                                       }

                                       if(!threadServerSocket.IsValueCreated)
                                       {
                                           threadServerSocket.Value = responseSocket = new ResponseSocket();
                                           responseSocket.Connect(_dealerSocket);
                                           responseSocket.ReceiveReady += HandleRequest;
                                           serverPoller.Add(responseSocket);

                                           SafeConsole.WriteLine($"Added thread lockal socket nr: {threadServerSocket.Values.Count}");
                                       }

                                       SafeConsole.WriteLine($"Server: {serverId} started");
                                   });
                      });
        }

        static long _totalRequests = 0;

        static void StartBroker() =>
            Task.Run(() =>
                     {
                         using(var router = new RouterSocket())
                         using(var dealer = new DealerSocket())
                         {
                             router.Bind(_routerSocket);
                             dealer.Bind(_dealerSocket);
                             new Proxy(router, dealer).Start();
                         }
                     });

        static void StartClients() =>
            1.Through(NumberOfClient)
             .ForEach(clientId =>
                      {
                          Task.Run(() =>
                                     {
                                         using(var requestSocket = new RequestSocket())
                                         {
                                             requestSocket.Connect(_routerSocket);
                                             SafeConsole.WriteLine($"Client: {clientId} started");
                                             while(true)
                                                 try
                                                 {
                                                     var request = new NetMQMessage();
                                                     request.Append(clientId);

                                                     //SafeConsole.WriteLine($"Client:{clientId} sending: {clientId}");
                                                     requestSocket.SendMultipartMessage(request);
                                                     var response = requestSocket.ReceiveMultipartMessage()
                                                                                 .First.ConvertToString();

                                                     //SafeConsole.WriteLine($"Client:{clientId} got: {response}");

                                                     if (!response.Contains($"Client:{clientId}"))
                                                     {
                                                         throw new Exception($"Client:{clientId} got response indended for another client: {response}");
                                                     }

                                                     //if(requests % 1000 == 0)
                                                     //{
                                                     //    SafeConsole.WriteLine($"Client:{clientId} has made {requests} requests.");
                                                     //}

                                                     var currentTotalRequests = Interlocked.Increment(ref _totalRequests);
                                                     if(currentTotalRequests % 10000 == 0)
                                                     {
                                                         SafeConsole.WriteLine($"Total requests: {currentTotalRequests:N}");
                                                     }


                                                 }
                                                 catch(Exception e)
                                                 {
                                                     Log.Error(e);
                                                 }
                                         }
                                         // ReSharper disable once FunctionNeverReturns
                                     });
                      });
    }
}
