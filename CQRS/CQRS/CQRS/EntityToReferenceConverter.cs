using System;
using Composable.DDD;

namespace Composable.CQRS
{
    public static class EntityToReferenceConverter
    {
         public static EntityReference<TEntity> AsReference<TEntity>(this TEntity me) where TEntity : IHasPersistentIdentity<Guid>, INamed
         {
             return new EntityReference<TEntity>(me.Id);
         } 
    }
}