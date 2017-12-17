using System;
using AccountManagement.Domain;

namespace AccountManagement.UI.QueryModels
{
  using Composable.Messaging.Events;

  class AccountQueryModel : ISingleAggregateQueryModel
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
