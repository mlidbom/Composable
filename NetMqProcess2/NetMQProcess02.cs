using Composable.Logging;
using NetMqProcess02._01_Introduction;

namespace NetMqProcess02
{
    static class NetMqProcess02
    {
        static void Main()
        {
            SafeConsole.WriteLine(nameof(NetMqProcess02));
            Client.Run();
        }
    }
}
