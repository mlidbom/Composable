using System;

namespace Composable.CQRS.Query.Models.Generators
{
    public interface IQueryModelGenerator { }


    public interface IQueryModelGenerator<out TDocument> : IQueryModelGenerator
    {
        TDocument TryGenerate(Guid id);
    }    
}