using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class ServiceBus : IServiceBus
    {
        readonly Outbox _outbox;

        public ServiceBus(Outbox transport) => _outbox = transport;

        public void SendAtTime(DateTime sendAt, IDomainCommand command) => _outbox.SendAtTime(sendAt, command);

        public void Send(IDomainCommand command)
        {
            CommandValidator.AssertCommandIsValid(command);
            _outbox.Send(command);
        }

        public void Publish(IEvent anEvent) => _outbox.Publish(anEvent);

        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return await _outbox.SendAsync(command);
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
            =>  await _outbox.QueryAsync(query);

        public TResult Query<TResult>(IQuery<TResult> query)
            => _outbox.Query(query);


        static class CommandValidator
        {
            public static void AssertCommandIsValid(IDomainCommand command)
            {
                var failures = ValidationFailures(command);
                if(failures.Any())
                {
                    throw new CommandValidationFailureException(failures);
                }
            }

            static IEnumerable<ValidationResult> ValidationFailures(object command)
            {
                var context = new ValidationContext(command, serviceProvider: null, items: null);
                var results = new List<ValidationResult>();

                Validator.TryValidateObject(command, context, results, validateAllProperties: true);
                return results;
            }
        }

    }

    public class CommandValidationFailureException : Exception
    {
        public CommandValidationFailureException(IEnumerable<ValidationResult> failures) {  }
    }
}
