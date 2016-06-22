using System;
using System.Diagnostics.Contracts;

namespace Composable
{
    ///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
    [Obsolete("Please use Composable.System.Disposable instead. This class will be removed soon.")]
    public class DisposeAction : IDisposable
    {
        private readonly Action _action;

        ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
        public DisposeAction(Action action)
        {
            Contract.Requires(action != null);
            _action = action;
        }

        ///<summary>Invokes the action passed to the constructor.</summary>
        public void Dispose()
        {
            _action();
        }

        ///<summary>Constructs an object that will call <param name="action"> when disposed.</param></summary>
        public static IDisposable Create(Action action)
        {
            return new DisposeAction(action);
        }
    }
}