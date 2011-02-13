using System;
using Composable.CQRS;

namespace SomeQueryClient
{
    public class ActiveCandidatesNamed : IQuery<CandidateListEntry>
    {

    }

    public class CandidateListEntry
    {
        public string Forename { get; private set; }
    }

}

namespace Composable.CQRS
{

    public interface IQueryService
    {
        TResult Execute<TResult, TQuery>(TQuery query) where TQuery : IQuery<TResult>;
    }

    public interface IQuery<TResult>
    {
    }

    public class TranslatingQueryService : IQueryService
    {
        private IQueryService _service;

        public TResult Execute<TResult, TQuery>(TQuery query) where TQuery : IQuery<TResult>
        {
            return Translate(_service.Execute<TResult, TQuery>(query));
        }

        private static TResult Translate<TResult>(TResult result)
        {
            throw new NotImplementedException();
        }
    }
}