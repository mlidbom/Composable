using System;
using NetMqProcess01._01_Introduction;

namespace NetMqProcess01
{
    static class NetMqProcess01
    {
        static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess01));
            new Server().Run();
        }
    }
}
