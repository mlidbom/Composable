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
        void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument);
        int Remove(string idString, IImmutableSet<Guid> acceptableTypes);
        //Urgent: This whole Guid vs string thing must be removed.
        IEnumerable<Guid> GetAllIds(IImmutableSet<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IImmutableSet<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IImmutableSet<Guid> acceptableTypes);

        class ReadRow
        {
            public ReadRow(Guid typeGuid, string serializedValue)
            {
                TypeGuid = typeGuid;
                SerializedValue = serializedValue;
            }

            public Guid TypeGuid { get; }

            public string SerializedValue { get; }
        }

        class WriteRow
        {
            public WriteRow(string idString, string serializedDocument, DateTime updateTime, Guid typeIdGuid)
            {
                IdString = idString;
                SerializedDocument = serializedDocument;
                UpdateTime = updateTime;
                TypeIdGuid = typeIdGuid;
            }

            public string IdString { get; }
            public string SerializedDocument { get; }
            public DateTime UpdateTime { get; }
            public Guid TypeIdGuid { get; }
        }
    }
}
