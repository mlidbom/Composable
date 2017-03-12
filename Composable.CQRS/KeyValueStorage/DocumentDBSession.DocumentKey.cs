using System;
using Composable.System;

namespace Composable.CQRS.KeyValueStorage
{
    public partial class DocumentDbSession
    {
        internal class DocumentKey : IEquatable<DocumentKey>
        {
            public DocumentKey(object id, Type type)
            {
                if(type.IsInterface)
                {
                    throw new ArgumentException("Since a type can implement multiple interfaces using it to uniquely identify an instance is impossible");
                }
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

            public override int GetHashCode() => Id.GetHashCode();

            public override string ToString() => "Id: {0}, Type: {1}".FormatWith(Id, Type);

            public string Id { get; }
            Type Type { get;  }

        }

        internal class DocumentKey<TDocument> : DocumentKey
        {
            public DocumentKey(object id) : base(id, typeof(TDocument)) { }
        }

    }
}
