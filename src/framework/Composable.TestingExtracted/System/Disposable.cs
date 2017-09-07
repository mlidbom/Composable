using System;
using Composable.Contracts;

namespace Composable.System
{
    ///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
    class Disposable : IDisposable
    {
        readonly Action _action;

        ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
        public Disposable(Action action)
        {
            Contract.Argument(() => action).NotNull();
            _action = action;
        }

        ///<summary>Invokes the action passed to the constructor.</summary>
        public void Dispose()
        {
            _action();
        }

        ///<summary>Constructs an object that will call <param name="action"> when disposed.</param></summary>
        public static IDisposable Create(Action action) => new Disposable(action);
    }
}