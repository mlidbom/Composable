#region usings

using System.Diagnostics.Contracts;

#endregion

namespace Composable.GenericAbstractions.Wrappers
{
    /// <summary>
    /// The most simple imaginable implementation of <see cref="IWrapper{T}"/>
    /// </summary>
    [Pure]
    public class GenericWrapper<T> : IWrapper<T>
    {
        /// <inheritdoc />
        public T Wrapped { get; private set; }

        /// <summary>Constructs an instance where <see cref="Wrapped"/> is <paramref name="wrapped"/> </summary>
        public GenericWrapper(T wrapped)
        {
            Wrapped = wrapped;
        }
    }
}