using System;
using Composable.DDD;

namespace CQRS.Tests.KeyValueStorage
{
    public class User : IPersistentEntity<Guid>
    {
        public Guid Id { get; set;  }
        public string Email { get; set; }
        public string Password { get; set; }

        public Address Address { get; set; }
    }

    public class Address : ValueObject<Address>
    {
        public string Street { get; set; }
        public int Streetnumber { get; set; }
        public string City { get; set; }
    }
}