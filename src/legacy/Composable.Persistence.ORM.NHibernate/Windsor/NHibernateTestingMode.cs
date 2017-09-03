namespace Composable.Persistence.ORM.NHibernate.Windsor
{
    public enum NHibernateTestingMode
    {
        ///<summary>Simply uses a collection that stores the actual instances of persisted objects. Fast but has somewhat different behavior than actual nhibernate.</summary>
        Collection,
        ///<summary>Uses an in-memory InMemorySqlite database when testing. More true to life, but much slower than <see cref="Collection"/> and might have issues with the entire database going missing if sessions are disposed.</summary>
        InMemorySqlite
    }
}