using System;
using System.Threading;
using Composable.Logging;

namespace Composable.CQRS.Tests
{
    static class AppDomainExtensions
    {
        public static void ExecuteInCloneDomainScope(CrossAppDomainDelegate action, TimeSpan? disposeDelay = null, bool suppressUnloadErrors = false)
        {
            using (var cloneDomainContext = AppDomain.CurrentDomain.CloneScope(disposeDelay: disposeDelay, suppressUnloadErrors: suppressUnloadErrors))
            {
                cloneDomainContext.CloneDomain.DoCallBack(action);
            }
        }

        static AppDomainScope CloneScope(this AppDomain me, TimeSpan? disposeDelay = null, bool suppressUnloadErrors = false)
        {
            var setup = new AppDomainSetup()
                        {
                            ApplicationBase = me.BaseDirectory,
                            ConfigurationFile = me.SetupInformation.ConfigurationFile
                        };


            return new AppDomainScope(
                AppDomain.CreateDomain(
                    friendlyName: "Test domain",
                    securityInfo: me.Evidence,
                    info: setup
                    ),
                disposeDelay,
                suppressUnloadErrors);
        }
    }

    class AppDomainScope : IDisposable
    {
        static readonly ILogger Log = Logger.For<AppDomainScope>();
        readonly TimeSpan? _disposeDelay;
        readonly bool _suppressUnloadErrors;

        public AppDomainScope(AppDomain cloneDomain, TimeSpan? disposeDelay, bool suppressUnloadErrors = false)
        {
            _disposeDelay = disposeDelay;
            _suppressUnloadErrors = suppressUnloadErrors;
            CloneDomain = cloneDomain;
        }

        public void Dispose()
        {
            if(_disposeDelay.HasValue)
            {
                Thread.Sleep(_disposeDelay.Value);
            }
            try
            {
                AppDomain.Unload(CloneDomain);
            }
            catch(Exception exception)
            {
                if(_suppressUnloadErrors)
                {
                    Log.Error(exception, "############ ERROR UNLOADING APP DOMAIN ###############");
                    return;
                }
                throw;
            }
        }

        public AppDomain CloneDomain { get; private set; }
    }
}
