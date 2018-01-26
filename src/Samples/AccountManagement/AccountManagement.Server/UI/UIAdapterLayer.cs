using Composable.Messaging.Buses;

namespace AccountManagement.UI
{
    static class UIAdapterLayer
    {
        public static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            AccountUIAdapter.GetById(registrar);
            AccountUIAdapter.Register(registrar);
            AccountUIAdapter.ChangeEmail(registrar);
            AccountUIAdapter.ChangePassword(registrar);
            AccountUIAdapter.Login(registrar);
        }
    }
}
