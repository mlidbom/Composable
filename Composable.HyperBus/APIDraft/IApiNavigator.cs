using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Composable.HyperBus.APIDraft
{
    public interface IApiNavigator<TResource>
    {        
        Task<IMessageCascade<TResource>> RunAndReturnCascadeAsync();
        Task<TResource> RunAsync();
        Task<TResource> RunAsync(NavigationOptions options);

        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<IQuery<TReturnResource>> getQuery);        
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TResource, IQuery<TReturnResource>> selectQuery);

        IApiNavigator<EmptyResource> Execute(Func<ICommand> getQuery);
        IApiNavigator<TReturnResource> Execute<TReturnResource>(Func<ICommand<TReturnResource>> selectCommand);
        IApiNavigator<TReturnResource> Execute<TReturnResource>(Func<TResource, ICommand<TReturnResource>> selectCommand);
    }
}