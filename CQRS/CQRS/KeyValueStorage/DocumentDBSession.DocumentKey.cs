using System;
using Composable.System;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession
    {
        public abstract class DocumentKey : IEquatable<DocumentKey>
        {
            protected DocumentKey(object id, Type type)
            {
                Id = id.ToString().ToLower().TrimEnd(' ');
                Type = type;
            }

            public bool Equals(DocumentKey other)
            {
                if(!Equals(Id, other.Id))
                {
                    return false;
                }

                return Type.IsAssignableFrom(other.Type) || other.Type.IsAssignableFrom(Type);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (!(obj is DocumentKey))
                {
                    return false;
                }
                return Equals((DocumentKey)obj);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            override public string ToString()
            {
                return "Id: {0}, Type: {1}".FormatWith(Id, Type);
            }

            public string Id { get; private set; }
            public Type Type { get; private set; }

            public abstract void RemoveFromStore(IObjectStore store);

        }

        public class DocumentKey<TDocument> : DocumentKey
        {
            public DocumentKey(object id) : base(id, typeof(TDocument)) { }
            override public void RemoveFromStore(IObjectStore store)
            {
                store.Remove<TDocument>(Id);
            }
        }

    }
}
