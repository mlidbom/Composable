#region usings

using System;


#endregion

namespace Composable.GenericAbstractions.Wrappers
{
    /// <summary>
    /// Represents the generic concept of a type that extends another type by containing a value of the other type.
    /// </summary>
    public interface IWrapper<T>
    {
        ///<summary>The wrapped value.</summary>
        T Wrapped { get; }
    }
}