#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS;

#endregion

namespace Composable.Persistence
{
    [Obsolete("Please use either IEntityReader, or IEntityPersister")]
    public interface IPersistenceSession : IEntityReader, IEntityPersister, IDisposable
    {
    }
}