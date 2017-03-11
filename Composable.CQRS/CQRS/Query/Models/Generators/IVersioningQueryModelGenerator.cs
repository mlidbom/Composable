using System;

namespace Composable.CQRS.CQRS.Query.Models.Generators
{
    interface IVersioningQueryModelGenerator : IQueryModelGenerator
    {
    }

    interface IVersioningQueryModelGenerator<out TDocument> : IVersioningQueryModelGenerator
    {
        TDocument TryGenerate(Guid id, int version);
    }
}