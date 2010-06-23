namespace Void
{
    public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
    {
        public virtual int Version { get; private set; }
    }
}