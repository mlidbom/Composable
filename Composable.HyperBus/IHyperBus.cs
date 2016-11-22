using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable 1591
namespace Composable.HyperBus
{
    public interface IMessage
    {
        Guid Id { get; }
    }

    public interface IEvent : IMessage {}

    public interface ICommand : IMessage {}

    public interface ICommand<TReturnValue> : ICommand {}

    public interface IQuery<TReturnValue> : IMessage {}

    public interface ICascade
    {
        IHyperBus Bus { get; }
        IMessage Message { get; }
    }

    public interface ICascade<TReturnValue>
    {
        Task<TReturnValue> GetReturnValue();
        Task RunToEndOfActivation(IEnumerable<Guid> excludedEndpoints = null, IEnumerable<Guid> includedEndpoints = null);
    }

    public interface IHyperBus
    {
        Task<ICascade> PublishAsync(IEvent @event);
        Task<ICascade> ExecuteAsync(ICommand command);
        Task<ICascade<TReturnValue>> ExecuteAsync<TReturnValue>(ICommand<TReturnValue> command);
        Task<ICascade<TReturnValue>> GetAsync<TReturnValue>(IQuery<TReturnValue> query);
    }

    public class Command : ICommand { public Guid Id { get; } }

    public class Command<TReturnValue> : Command, ICommand<TReturnValue> { }


    public class EmptyResource { }

    public interface IApiNavigator<TResource>
    {        
        Task<ICascade<TResource>> RunAndReturnCascadeAsync();
        Task<TResource> RunAsync();
        Task<TResource> RunAsync(NavigationOptions options);

        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<IQuery<TReturnResource>> linkSelector);        
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TResource, IQuery<TReturnResource>> linkSelector);

        IApiNavigator<EmptyResource> Execute(Func<ICommand> linkSelector);
        IApiNavigator<TReturnResource> Execute<TReturnResource>(Func<ICommand<TReturnResource>> linkSelector);
        IApiNavigator<TReturnResource> Execute<TReturnResource>(Func<TResource, ICommand<TReturnResource>> linkSelector);
    }

    public class ApiNavigator
    {
        public static IApiNavigator<TResource> Get<TResource>(IQuery<TResource> query, IHyperBus bus) { return null; }
    }

    public enum NavigationOptions
    {
        AwaitCascadeActivationCompletion
    }

    public class RegisterAccountCommand : Command<Account>
    {}


    public class Contact { }

    public class Account
    {
        public  LinksClass Links { get; }
        public CommandsClass Commands { get; }
        public class LinksClass
        {
            public IQuery<Contact> Contact { get; }
        }

        public class CommandsClass
        {
            public ChangeEmailCommand ChangeEmail(string email) => new ChangeEmailCommand() ;
            public class ChangeEmailCommand : Command
            {
                public string Email { get;}
            }
        }
    }



    public class MyApplicationAPIStartResource
    {
        public static IQuery<MyApplicationAPIStartResource> Self { get; }
        public LinksClass Links { get; } = new LinksClass();

        public class LinksClass
        {
            public IQuery<AccountsAPIIndexResource> Accounts { get; }
        }
    }

    public class AccountsAPIIndexResource
    {
        public static IQuery<AccountsAPIIndexResource> Self { get; }
        public CommandsClass Commands { get; } = new CommandsClass();

        public class CommandsClass
        {
            public RegisterAccountCommand Register(string email, string password) => new RegisterAccountCommand(); 
        }
    }

    public class Demo
    {
        private IHyperBus Bus { get; }
        private IApiNavigator<EmptyResource> Navigator { get; }

        public async Task DemoNavigatorUsage()
        {
            var account = await Navigator.Get(() => MyApplicationAPIStartResource.Self)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password:"secret"))
                                        .RunAsync();

            var contact = await Navigator.Get(() => account.Links.Contact).RunAsync();

            await Navigator.Execute(() => account.Commands.ChangeEmail("new@email.com")).RunAsync();
        }

        public async Task DemoNavigatorUsageÁndAwaitingCascade()
        {
            var account = await Navigator.Get(() => MyApplicationAPIStartResource.Self)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password: "secret"))
                                        .RunAsync(NavigationOptions.AwaitCascadeActivationCompletion);           
        }
         
        public async Task DemoNavigatorUsageÁndAwaitingCascadeManually()
        {
            var apiNavigationSpecification = Navigator.Get(() => MyApplicationAPIStartResource.Self)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password: "secret"));

            var accountCreationCascade = await apiNavigationSpecification.RunAndReturnCascadeAsync();


            await accountCreationCascade.RunToEndOfActivation(); //Await complete activation
            await accountCreationCascade.RunToEndOfActivation(excludedEndpoints: new[] { Guid.Parse("13AF286B-1303-4028-A4FB-E32D7C456D99") }); //Wait for complete activation excepting the specified endpoints
            await accountCreationCascade.RunToEndOfActivation(includedEndpoints: new[] { Guid.Parse("13AF286B-1303-4028-A4FB-E32D7C456D99") }); //Only wait for specific endpoints to be done.            
            var account = await accountCreationCascade.GetReturnValue();
        }


        public async Task DemoDirectBusUsage()
        {
            var startPageSomething = await Bus.GetAsync(MyApplicationAPIStartResource.Self);
            var startPage = await startPageSomething.GetReturnValue();
            var accountsSomething = await Bus.GetAsync(startPage.Links.Accounts);
            var accounts = await accountsSomething.GetReturnValue();
            var account = await Bus.ExecuteAsync(accounts.Commands.Register(email: "someone@somewhere.com", password: "secret"));
        }
    }

}
