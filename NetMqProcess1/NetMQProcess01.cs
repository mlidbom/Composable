using System;

namespace NetMqProcess01
{
    static class NetMqProcess01
    {
        static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess01));
            //_01_Introduction.Server.Run();
            _50_Router_Dealer.RouterAndDealer.Run();
        }
    }
}
