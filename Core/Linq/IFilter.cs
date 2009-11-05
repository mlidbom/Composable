using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Void.Linq
{
    public interface IFilter<T>
    {
        IEnumerable<Expression<Func<T, Boolean>>> Filters { get; }
    }
}