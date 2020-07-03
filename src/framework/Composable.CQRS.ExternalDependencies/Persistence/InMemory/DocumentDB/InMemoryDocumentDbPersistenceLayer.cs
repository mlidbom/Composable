using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.DocumentDb;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Persistence.InMemory.DocumentDB
{
    class InMemoryDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly Dictionary<string, List<DocumentRow>> _db = new Dictionary<string, List<DocumentRow>>(StringComparer.InvariantCultureIgnoreCase);
        readonly object _lockObject = new object();

        //Urgent: Take a WriteRow
        public void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument)
        {
            lock (_lockObject)
            {
                if (Contains(typeIdGuid, idString))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(idString, serializedDocument);
                }
                _db.GetOrAddDefault(idString).Add(new DocumentRow(idString, typeIdGuid, now, serializedDocument));
            }
        }

        public bool TryGet(string idString, IReadOnlyList<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? value)
        {
            lock (_lockObject)
            {
                value = null;
                if(!_db.TryGetValue(idString, out var matchesId))
                {
                    return false;
                }


                var found = matchesId.Where(@this => acceptableTypeIds.Contains(@this.TypeId) ).ToList();
                if(found.Any())
                {
                    var documentRow = found.Single();
                    value = new IDocumentDbPersistenceLayer.ReadRow(documentRow.TypeId, documentRow.SerializedDocument);
                    return true;
                }

                return false;
            }
        }

        public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate)
        {
            lock (_lockObject)
            {
                foreach(var row in toUpdate)
                {
                    if (!TryGet(row.IdString, new []{ row.TypeIdGuid }, useUpdateLock: false, out var existing)) throw new NoSuchDocumentException(row.IdString, row.TypeIdGuid);
                    if (existing.SerializedValue != row.SerializedDocument)
                    {
                        Remove(row.IdString, new []{ row.TypeIdGuid });
                        Add(row.IdString,row.TypeIdGuid, row.UpdateTime, row.SerializedDocument);
                    }
                }
            }
        }

        public int Remove(string idstring, IReadOnlyList<Guid> acceptableTypes)
        {
            lock (_lockObject)
            {
                var removed = _db.GetOrAddDefault(idstring).RemoveWhere(@this => acceptableTypes.Contains(@this.TypeId));
                if (removed.None()) throw new NoSuchDocumentException(idstring, acceptableTypes.First());
                if (removed.Count > 1) throw new Exception("It really should be impossible to hit multiple documents with one Id, but apparently you just did it!");

                return 1;
            }
        }

        public IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypes)
        {
            var typeIds = new HashSet<Guid>(acceptableTypes);
            lock (_lockObject)
            {
                return _db
                      .SelectMany(@this => @this.Value)
                      .Where(@this => typeIds.Contains(@this.TypeId))
                      .Select(@this =>
                       {
                           Guid.TryParse(@this.Id, out var id);
                           return id;
                       })
                      .Where(@this => @this != Guid.Empty)
                      .ToList();
            }
        }

        //Urgent: pass ISet<Guid> for both params
        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> acceptableTypes)
        {
            var typeIds = new HashSet<Guid>(acceptableTypes);
            lock (_lockObject)
            {
                return _db
                      .SelectMany(@this => @this.Value)
                      .Where(@this =>  typeIds.Contains(@this.TypeId) && Guid.TryParse(@this.Id, out var myId) && ids.Contains(myId))
                      .Select(@this => new IDocumentDbPersistenceLayer.ReadRow(@this.TypeId, @this.SerializedDocument))
                      .ToList();
            }
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypes)
        {
            var typeIds = new HashSet<Guid>(acceptableTypes);
            lock (_lockObject)
            {
                return _db
                      .SelectMany(@this => @this.Value)
                      .Where(@this => typeIds.Contains(@this.TypeId))
                      .Select(@this => new IDocumentDbPersistenceLayer.ReadRow(@this.TypeId, @this.SerializedDocument))
                      .ToList();
            }
        }

        bool Contains(Guid type, string id) => TryGet(id, new[]{ type }, false, out _);

        class DocumentRow
        {
            public DocumentRow(string id, Guid typeId, DateTime updateTime, string serializedDocument)
            {
                Id = id;
                TypeId = typeId;
                UpdateTime = updateTime;
                SerializedDocument = serializedDocument;
            }

            public string Id { get; }
            public Guid TypeId { get; }
            public DateTime UpdateTime { get; }
            public string SerializedDocument { get; }
        }
    }
}
