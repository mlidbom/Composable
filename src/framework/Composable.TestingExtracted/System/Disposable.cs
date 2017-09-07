using System;
using Composable.Testing.Contracts;

namespace Composable.Testing.System
{
    ///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
    class Disposable : IDisposable
    {
        readonly Action _action;

        ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
        public Disposable(Action action)
        {
            Contract.AssertThat(action != null);
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