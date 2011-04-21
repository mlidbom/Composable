using System;
using Composable.DDD;

namespace Manpower.System.Web.Mvc
{
    public class Command : ValueObject<Command>
    {
        public Guid Id { get; set; }
        protected Command()
            : this(Guid.NewGuid())
        {
        }

        protected Command(Guid id)
        {
            Id = id;
        }
    }
}