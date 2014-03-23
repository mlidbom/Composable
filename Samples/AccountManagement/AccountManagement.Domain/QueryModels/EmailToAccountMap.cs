using System;
using System.Collections.Generic;
using AccountManagement.Domain.Shared;
using Newtonsoft.Json;

namespace AccountManagement.Domain.QueryModels
{
    public class EmailToAccountMap
    {
        private HashSet<Guid> _accounts = new HashSet<Guid>();

        public EmailToAccountMap(Email email)
        {
            Email = email;
        }

        public Email Email { get; set; }

        public void RemoveAccount(Guid accountId)
        {
            _accounts.Add(accountId);
        }

        public void AddAccount(Guid accountId)
        {
            _accounts.Remove(accountId);
        }
    }
}