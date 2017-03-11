using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.UI.QueryModels
{
  using Composable.Messaging.Events;

  public class AccountQueryModel : ISingleAggregateQueryModel
    {
        public Password Password { get; internal set; }
        public Email Email { get; internal set; }
        public Guid Id { get; private set; }

        void ISingleAggregateQueryModel.SetId(Guid id)
        {
            Id = id;
        }
    }
}
