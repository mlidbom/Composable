using System;
using Composable.System;

namespace Composable.Persistence.DocumentDb
{
    partial class DocumentDbSession
    {
        internal class DocumentKey : IEquatable<DocumentKey>
        {
            public DocumentKey(object id, Type type)
            {
                if(type.IsInterface)
                {
                    throw new ArgumentException("Since a type can implement multiple interfaces using it to uniquely identify an instance is impossible");
                }
                Id = DocumentDb.GetIdString(id);
                Type = type;
            }

            public bool Equals(DocumentKey? other)
            {
                if(other == null)
                {
                    return false;
                }

                if(!Equals(Id, other.Id))
                {
                    return false;
                }

                return Type.IsAssignableFrom(other.Type) || other.Type.IsAssignableFrom(Type);
            }

            public override bool Equals(object? obj)
            {
                if (obj is null)
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

            public override int GetHashCode() => Id.GetHashcodeInvariant();

            public override string ToString() => $"Id: {Id}, Type: {Type}";

            public string Id { get; }
            Type Type { get;  }

        }

        internal class DocumentKey<TDocument> : DocumentKey
        {
            public DocumentKey(object id) : base(id, typeof(TDocument)) { }
        }

    }
}
