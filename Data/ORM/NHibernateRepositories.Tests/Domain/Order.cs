using System.Collections.Generic;
using Void.Data.ORM.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests.Domain
{
    public class Order : PersistentEntityWithSurrogateKey<Order,int>
    {
        public virtual IList<OrderLine> Lines { get; set; }
    }
}