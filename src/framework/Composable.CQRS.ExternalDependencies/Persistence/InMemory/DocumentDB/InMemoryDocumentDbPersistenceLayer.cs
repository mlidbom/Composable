using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Persistence.DocumentDb;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using DocumentRow = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.WriteRow;
namespace Composable.Persistence.InMemory.DocumentDB
{
    //Urgent: Transactional locks and transactional overlay
    class InMemoryDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        readonly Dictionary<string, List<DocumentRow>> _db = new Dictionary<string, List<DocumentRow>>(StringComparer.InvariantCultureIgnoreCase);
        readonly object _lockObject = new object();

        public void Add(IDocumentDbPersistenceLayer.WriteRow row)
        {
            lock (_lockObject)
            {
                if (Contains(row.TypeId, row.Id))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
                }
                _db.GetOrAddDefault(row.Id).Add(row);
            }
        }

        public bool TryGet(string idString, IImmutableSet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? value)
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

        public void Update(IReadOnlyList<DocumentRow> toUpdate)
        {
            lock (_lockObject)
            {
                foreach(var row in toUpdate)
                {
                    if (!TryGet(row.Id, new []{ row.TypeId }.ToImmutableHashSet(), useUpdateLock: false, out var existing)) throw new NoSuchDocumentException(row.Id, row.TypeId);
                    if (existing.SerializedValue != row.SerializedDocument)
                    {
                        Remove(row.Id, new []{ row.TypeId }.ToImmutableHashSet());
                        Add(row);
                    }
                }
            }
        }

        public int Remove(string idstring, IImmutableSet<Guid> acceptableTypes)
        {
            lock (_lockObject)
            {
                var removed = _db.GetOrAddDefault(idstring).RemoveWhere(@this => acceptableTypes.Contains(@this.TypeId));
                if (removed.None()) throw new NoSuchDocumentException(idstring, acceptableTypes.First());
                if (removed.Count > 1) throw new Exception("It really should be impossible to hit multiple documents with one Id, but apparently you just did it!");

                return 1;
            }
        }

        public IEnumerable<Guid> GetAllIds(IImmutableSet<Guid> acceptableTypes)
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

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IImmutableSet<Guid> acceptableTypes)
        {
            lock (_lockObject)
            {
                return _db
                      .SelectMany(@this => @this.Value)
                      .Where(@this =>  acceptableTypes.Contains(@this.TypeId) && Guid.TryParse(@this.Id, out var myId) && ids.Contains(myId))
                      .Select(@this => new IDocumentDbPersistenceLayer.ReadRow(@this.TypeId, @this.SerializedDocument))
                      .ToList();
            }
        }

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IImmutableSet<Guid> acceptableTypes)
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

        bool Contains(Guid type, string id) => TryGet(id, new[]{ type }.ToImmutableHashSet(), false, out _);
    }
}
