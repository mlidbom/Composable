using System;

namespace NetMqProcess01
{
    static class NetMqProcess01
    {
        static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess01));
            new NetMqProcess1._01_Introduction.Server().Run();
        }
    }
}
