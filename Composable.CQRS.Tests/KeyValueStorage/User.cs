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
        public User()
        {
            Id = Guid.NewGuid();
        }

        public string Email { get; set; } = "some.email@nodomain.not";
        public string Password { get; set; } = "default";

        public Address Address { get; set; } = new Address();

        public HashSet<User> People { get; set; }
    }

    class Address : ValueObject<Address>
    {
        public string Street { [UsedImplicitly] get; set; } = "Somestreet";
        public int Streetnumber { [UsedImplicitly] get; set; } = 12;
        public string City { [UsedImplicitly] get; set; } = "Ostnahe";
    }

    class Email : ValueObject<Email>
    {
        public Email(string email) => TheEmail = email;
        public string TheEmail { get; private set; }
    }
}