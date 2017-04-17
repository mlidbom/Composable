using System;
using AccountManagement.Domain;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AccountManagement.UI.QueryModels
{
    public class EmailToAccountMapQueryModel
    {
        [UsedImplicitly] EmailToAccountMapQueryModel() {}
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Email = email;
            AccountId = accountId;
        }

        [JsonProperty] Email Email { [UsedImplicitly] get; set; }
        [JsonProperty] internal Guid AccountId { get; private set; }
    }
}
