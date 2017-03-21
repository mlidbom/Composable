using System;
using System.Linq;
using Composable.Logging;

namespace NetMqProcess01
{
    static class NetMqProcess01
    {
        public static void Main(string[] args)
        {
            int runType = 3;
            if(args.Any())//Really just a trick to keep unused classes from being detected by resharper :)
            {
                runType = int.Parse(args[0]);
            }
            SafeConsole.WriteLine(nameof(NetMqProcess01));
            switch(runType)
            {
                case 1:
                    _01_Introduction.Server.Run();
                    break;
                case 2:
                    _50_Router_Dealer.RouterAndDealer.Run();
                    break;
                case 3:
                    __Mine._01_ReqRouterDealerRep.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}
