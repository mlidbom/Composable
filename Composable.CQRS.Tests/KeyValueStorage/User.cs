using System;
using System.Collections.Generic;
using Composable.DDD;
using JetBrains.Annotations;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    class Person : ValueObject<Person>, IPersistentEntity<Guid>
    {
        public Guid Id { get; internal set;  }
    }

    class User : Person
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public Address Address { get; set; }

        public HashSet<User> People { get; set; }
    }

    class Address : ValueObject<Address>
    {
        public string Street { [UsedImplicitly] get; set; }
        public int Streetnumber { [UsedImplicitly] get; set; }
        public string City { [UsedImplicitly] get; set; }
    }

    class Email : ValueObject<Email>
    {
        public Email(string email) => TheEmail = email;
        public string TheEmail { get; private set; }
    }
}