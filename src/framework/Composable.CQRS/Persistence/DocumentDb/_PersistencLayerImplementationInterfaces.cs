using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.DocumentDb
{
    interface IDocumentDbPersistenceLayer
    {
        void Update(IReadOnlyList<WriteRow> toUpdate);
        bool TryGet(string idString, IImmutableSet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
        void Add(WriteRow row);
        int Remove(string idString, IImmutableSet<Guid> acceptableTypes);
        //Urgent: This whole Guid vs string thing must be removed.
        IEnumerable<Guid> GetAllIds(IImmutableSet<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IImmutableSet<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IImmutableSet<Guid> acceptableTypes);

        class ReadRow
        {
            public ReadRow(Guid typeId, string serializedDocument)
            {
                TypeId = typeId;
                SerializedDocument = serializedDocument;
            }

            public Guid TypeId { get; }

            public string SerializedDocument { get; }
        }

        class WriteRow
        {
            public WriteRow(string id, string serializedDocument, DateTime updateTime, Guid typeId)
            {
                Id = id;
                SerializedDocument = serializedDocument;
                UpdateTime = updateTime;
                TypeId = typeId;
            }

            public string Id { get; }
            public string SerializedDocument { get; }
            public DateTime UpdateTime { get; }
            public Guid TypeId { get; }
        }

        internal static class DocumentTableSchemaStrings
        {
            internal const string TableName = "Store";
            internal const string Id = nameof(Id);
            internal const string ValueTypeId = nameof(ValueTypeId);
            internal const string Created = nameof(Created);
            internal const string Updated = nameof(Updated);
            internal const string Value = nameof(Value);
        }
    }
}
