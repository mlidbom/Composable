using System.Reflection;
using System.Web;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle;

namespace Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb
{
    /// <summary>
    /// Hybrid lifestyle manager where the main lifestyle is <see cref = "PerWebRequestLifestyleManager" />
    /// </summary>
    /// <typeparam name = "T">Secondary lifestyle</typeparam>
    public class HybridPerWebRequestLifestyleManager<T> : HybridLifestyleManager<PerWebRequestLifestyleManager, T>
        where T : ILifestyleManager, new()
    {

        // TODO make this public in Windsor
        private static readonly PropertyInfo PerWebRequestLifestyleModuleInitialized = typeof(PerWebRequestLifestyleModule).GetProperty("Initialized", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool IsPerWebRequestLifestyleModuleInitialized
        {
            get
            {
                return (bool)PerWebRequestLifestyleModuleInitialized.GetValue(null, null);
            }
        }

        public override object Resolve(CreationContext context)
        {
            if (HttpContext.Current != null && IsPerWebRequestLifestyleModuleInitialized)
                return lifestyle1.Resolve(context);
            return lifestyle2.Resolve(context);
        }
    }
}