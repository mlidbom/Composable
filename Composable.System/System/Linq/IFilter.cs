#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Composable.System.Linq
{
    /// <summary>
    /// Implementing this interface enables you to  use the existing extensionmethods 
    /// 
    /// <see cref="Filter.Where{TItemType}(IEnumerable{TItemType},Composable.System.Linq.IFilter{TItemType})"/>
    /// and <see cref="Filter.Where{TItemType}(IQueryable{TItemType},Composable.System.Linq.IFilter{TItemType})"/>
    /// 
    /// This makes your implementing class as easy to use with linq as any simple predicate.
    /// 
    /// A typical usage would be to implement this interface in a class that 
    /// represents a search of some sort, enabling it to be used freely and easily in where clauses.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ContractClass(typeof(FilterContract<>))]
    public interface IFilter<T>
    {
        /// <summary>
        /// Implementers should return the predicates that they wish to filter 
        /// the sequence by.
        /// </summary>
        IEnumerable<Expression<Func<T, Boolean>>> Filters { get; }
    }

    [ContractClassFor(typeof(IFilter<>))]
    internal abstract class FilterContract<T> : IFilter<T>
    {
        /// <summary/>
        public IEnumerable<Expression<Func<T, bool>>> Filters
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Expression<Func<T, bool>>>>() != null);
                return null;
            }
        }
    }
}