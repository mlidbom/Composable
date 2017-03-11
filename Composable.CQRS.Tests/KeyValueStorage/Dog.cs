using System;
using Composable.DDD;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    class Dog : IPersistentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}