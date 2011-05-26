using System;
using Composable.DDD;

namespace CQRS.Tests.KeyValueStorage
{
    public class User : IPersistentEntity<Guid>
    {
        public Guid Id { get; set;  }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}