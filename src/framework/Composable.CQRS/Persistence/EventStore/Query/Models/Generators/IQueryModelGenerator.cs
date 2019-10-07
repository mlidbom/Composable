using System;
using Composable.Functional;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    public interface IQueryModelGenerator { }

    interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
    {
        Option<TDocument> TryGenerate(Guid id);
    }
}