using System;
using System.Threading.Tasks;

namespace Composable.SystemCE
{
    static class VoidCEExtensions
    {
        internal static Func<TParam, VoidCE> AsVoidFunc<TParam>(this Action<TParam> @this) =>
            param =>
            {
                @this(param);
                return VoidCE.Instance;
            };

        internal static Func<TParam, Task<VoidCE>> AsVoidFunc<TParam>(this Func<TParam, Task> @this) =>
            param =>
            {
                @this(param);
                return VoidCE.InstanceTask;
            };

        internal static Func<VoidCE> AsVoidFunc(this Action @this) =>
            () =>
            {
                @this();
                return VoidCE.Instance;
            };
    }
}
