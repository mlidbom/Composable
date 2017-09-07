using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Composable.Testing.System.Threading;
using JetBrains.Annotations;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool
    {
        [UsedImplicitly] class SharedState : IBinarySerializeMySelf
        {
            readonly List<Database> _databases = new List<Database>();
            IReadOnlyList<Database> Databases => _databases;

            internal Database Release(int id) => Get(id).Release();

            internal bool IsValid()
            {
                if(_databases.Count == 0)
                {
                    return false;
                }

                for (int i = 1; i <= _databases.Count; i++)
                {
                    if (i != _databases[i - 1].Id)
                    {
                        return false;
                    }
                }
                return true;
            }

            internal bool NeedsGarbageCollection()
            {
                var shouldBeGargarbageCollected = Databases.Where(db => db.EligibleForGarbageCollection).Count();
                var freeAndClear = Databases.Where(db => db.FreeAndClean).Count();

                if(shouldBeGargarbageCollected > freeAndClear)
                {
                    return true;
                }

                if(shouldBeGargarbageCollected > 40)
                {
                    return true;
                }

                if(freeAndClear < 20)
                {
                    return true;
                }

                return false;
            }

            internal IReadOnlyList<Database> ReserveDatabasesForGarbageCollection()
            {
                var toCollect = Databases.Where(db => db.EligibleForGarbageCollection).OrderBy(db => db.ReservationDate).Take(30).ToList();

                foreach(var database in toCollect)
                {
                    if(database.IsReserved)
                    {
                        database.Release();
                    }
                    database.Reserve("Garbage_collection_task", Guid.NewGuid());
                }

                return toCollect;
            }

            internal bool TryReserve(out Database reserved, string reservationName, Guid poolId)
            {
                var unreserved = _databases.Where(db => db.FreeAndClean)
                                           .OrderBy(db => db.ReservationDate)
                                           .ToList();

                reserved = unreserved.FirstOrDefault();
                if(reserved == null)
                {
                    return false;
                }
                reserved.Reserve(reservationName, poolId);
                return true;
            }

            internal IReadOnlyList<Database> DatabasesReservedBy(Guid poolId) => _databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId)
                                                                                           .ToList();

            internal Database Insert()
            {
                var database = new Database(_databases.Count + 1);
                _databases.Add(database);
                return database;
            }

            Database Get(int id) => _databases.Single(db => db.Id == id);

            internal void Reset()
            {
                _databases.Clear();
            }

            public void Deserialize(BinaryReader reader)
            {
                while(reader.ReadBoolean())//I use negative boolean to mark end of object
                {
                    var database = new Database();
                    database.Deserialize(reader);
                    _databases.Add(database);
                }
            }

            public void Serialize(BinaryWriter writer)
            {
                _databases.ForEach(db =>
                                  {
                                      writer.Write(true);
                                      db.Serialize(writer);
                                  });
                writer.Write(false);//use false to mark end of graph
            }
        }
    }
}
