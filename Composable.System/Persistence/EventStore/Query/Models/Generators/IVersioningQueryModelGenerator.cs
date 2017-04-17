using System;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    interface IVersioningQueryModelGenerator : IQueryModelGenerator
    {
    }

    interface IVersioningQueryModelGenerator<out TDocument> : IVersioningQueryModelGenerator
    {
        TDocument TryGenerate(Guid id, int version);
    }
}