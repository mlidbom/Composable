using System;

namespace Composable.CQRS.CQRS.Query.Models.Generators
{
    public interface IQueryModelGenerator { }

    interface IQueryModelGenerator<out TDocument> : IQueryModelGenerator
    {
        TDocument TryGenerate(Guid id);
    }
}