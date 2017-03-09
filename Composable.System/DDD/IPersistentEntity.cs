#region usings

using System;


#endregion

namespace Composable.DDD
{
    /// <summary>
    /// Should be implemented by persistent* classes that represents entities in the Domain Driven Design sense of the word.
    /// 
    /// The vital distinction about Persistent Entities is that equality is defined by Identity, 
    /// and as such they must guarantee that they have a non-default identity at all times.  
    /// 
    /// * Classes that have a lifecycle longer than an application run. Often persisted in databases.
    /// </summary>
    /// <typeparam name="TKeyType"></typeparam>    
    public interface IPersistentEntity<out TKeyType> : IHasPersistentIdentity<TKeyType>
    {        
    }
}