using System;
using Composable.Functional;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    interface IVersioningQueryModelGenerator : IQueryModelGenerator
    {
    }

    interface IVersioningQueryModelGenerator<TDocument> : IVersioningQueryModelGenerator
    {
        Option<TDocument> TryGenerate(Guid id, int version);
    }
}