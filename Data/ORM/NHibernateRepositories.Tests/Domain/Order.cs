#region usings

using System.Collections.Generic;
using Composable.Data.ORM.Domain;

#endregion

namespace Composable.Data.ORM.NHibernateRepositories.Tests.Domain
{
    public class Order : PersistentEntityWithSurrogateKey<Order, int>
    {
        public virtual IList<OrderLine> Lines { get; set; }
    }
}