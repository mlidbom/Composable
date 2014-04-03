using System;

namespace Composable.CQRS.Query.Models.Generators
{
    public interface IVersioningQueryModelGenerator : IQueryModelGenerator
    {
    }

    public interface IVersioningQueryModelGenerator<out TDocument> : IVersioningQueryModelGenerator
    {
        TDocument TryGenerate(Guid id, int version);
    }
}