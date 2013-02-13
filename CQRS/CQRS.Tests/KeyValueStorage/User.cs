using System;
using System.Collections.Generic;
using Composable.DDD;

namespace CQRS.Tests.KeyValueStorage
{
    public class Person : ValueObject<Person>, IPersistentEntity<Guid>
    {
        public string Name { get; set; }
        public Guid Id { get; set;  }
    }

    public class User : Person
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public Address Address { get; set; }

        public HashSet<User> People { get; set; }
    }

    public class Address : ValueObject<Address>
    {
        public string Street { get; set; }
        public int Streetnumber { get; set; }
        public string City { get; set; }
    }

    public class Email : ValueObject<Email>
    {
        public Email(string email)
        {
            TheEmail = email;
        }
        public string TheEmail { get; set; }
    }
}