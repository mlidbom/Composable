using System;
using NetMqProcess02._01_Introduction;

namespace NetMqProcess02
{
    static class NetMqProcess02
    {
        static void Main(string[] args)
        {
            Console.WriteLine(nameof(NetMqProcess02));
            new Client().Run();
        }
    }
}
