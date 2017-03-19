using System;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    public interface IQueryModelGenerator { }

    interface IQueryModelGenerator<out TDocument> : IQueryModelGenerator
    {
        TDocument TryGenerate(Guid id);
    }
}