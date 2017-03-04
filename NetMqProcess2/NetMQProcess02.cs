using System;

namespace NetMqProcess02
{
    static class NetMqProcess02
    {
        static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess02));
            new NetMqProcess2._01_Introduction.Client().Run();
        }
    }
}
