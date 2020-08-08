using System;
using System.Threading.Tasks;


namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
       static readonly object DummyObject = new object();
        static readonly Task<object> CompletedObjectTask = Task.FromResult(DummyObject);

        internal static Func<TParam, object> AsFunc<TParam>(this Action<TParam> @this) =>
            param =>
            {
                @this(param);
                return DummyObject;
            };

        internal static Func<TParam, Task<object>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
            param =>
            {
                @this(param);
                return CompletedObjectTask;
            };

        internal static Func<object> AsFunc(this Action @this) =>
            () =>
            {
                @this();
                return DummyObject;
            };
    }
}
