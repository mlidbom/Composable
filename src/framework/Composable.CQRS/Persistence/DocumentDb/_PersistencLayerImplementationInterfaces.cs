using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.DocumentDb
{
    interface IDocumentDbPersistenceLayer
    {
        void Update(IReadOnlyList<WriteRow> toUpdate);
        bool TryGet(string idString, IReadOnlyList<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
        void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument);
        int Remove(string idString, IReadOnlyList<Guid> acceptableTypes);
        IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> acceptableTypes);
        IReadOnlyList<ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypeIds);

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
