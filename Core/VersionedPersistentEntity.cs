namespace Void
{
    ///<summary>Base class for persistent entities with versioning information</summary>
    public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
    {
        ///<summary>Contains the current version of the entity</summary>
        public virtual int Version { get; private set; }
    }
}