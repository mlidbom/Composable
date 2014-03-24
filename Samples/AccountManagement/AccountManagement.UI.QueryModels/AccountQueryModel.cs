using System;
using AccountManagement.Domain.Shared;
using Composable.CQRS.EventHandling;
using Composable.DDD;

namespace AccountManagement.UI.QueryModels
{
    public class AccountQueryModel : ValueObject<AccountQueryModel>, ISingleAggregateQueryModel
    {
        public Password Password { get; set; }
        public Email Email { get; set; }
        public Guid Id { get; private set; }

        void ISingleAggregateQueryModel.SetId(Guid id)
        {
            Id = id;
        }
    }
}