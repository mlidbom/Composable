using System;

namespace NetMqProcess01
{
    public static class NetMqProcess01
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess01));
            //_01_Introduction.Server.Run();
            //_50_Router_Dealer.RouterAndDealer.Run();
            __Mine._01_ReqRouterDealerRep.Run();
        }
    }
}
