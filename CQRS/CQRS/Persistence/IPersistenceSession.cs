#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS;

#endregion

namespace Composable.Persistence
{
    public interface IPersistenceSession : IEntityFetcher, IEntityPersister, IDisposable
    {
    }
}