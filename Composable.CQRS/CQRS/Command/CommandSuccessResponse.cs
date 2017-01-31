using System;
using System.Collections.Generic;
using Composable.DomainEvents;
using System.Linq;
using Composable.NewtonSoft;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Composable.CQRS.Command
{
    public class CommandSuccessResponse : ICommandSuccessResponse
    {
        [Obsolete("Only for serialization", true), UsedImplicitly]
        public CommandSuccessResponse(){}

        public CommandSuccessResponse(Guid commandId, IEnumerable<IDomainEvent> events)
        {
            CommandId = commandId;
            _events = events.ToArray();
#pragma warning disable 612,618
            EventsSerialized = JsonConvert.SerializeObject(_events, JsonSettings.JsonSerializerSettings);
#pragma warning restore 612,618
        }

        public Guid CommandId { get; set; }

        private IDomainEvent[] _events = null;

        [Obsolete("Only for serialization")]
        public string EventsSerialized { get; set; }

        public IDomainEvent[] Events
        {
            get
            {
                if(_events == null)
                {
#pragma warning disable 612,618
                    _events = JsonConvert.DeserializeObject<IDomainEvent[]>(EventsSerialized, JsonSettings.JsonSerializerSettings); ;
#pragma warning restore 612,618
                }
                return _events.ToArray();
            }
        }
    }
}
