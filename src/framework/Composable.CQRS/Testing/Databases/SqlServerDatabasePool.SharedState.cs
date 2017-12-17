using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Composable.System.Linq;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool
    {
        [UsedImplicitly] class SharedState : IBinarySerializeMySelf
        {
            readonly List<Database> _databases = new List<Database>();
            IReadOnlyList<Database> Databases => _databases;

            Database Release(int id) => Get(id).Release();

            internal bool IsEmpty => _databases.Count == 0;

            internal bool IsValid()
            {
                if(_databases.Count == 0)
                {
                    return false;
                }

                for(var i = 1; i <= _databases.Count; i++)
                {
                    if(i != _databases[i - 1].Id)
                    {
                        return false;
                    }
                }
                return true;
            }

            internal bool TryReserve(out Database reserved, string reservationName, Guid poolId)
            {
                CollectGarbage();

                reserved = _databases.Where(db => !db.IsReserved)
                                     .OrderBy(db => db.ReservationDate)
                                     .FirstOrDefault();

                if(reserved == null)
                {
                    return false;
                }

                reserved.Reserve(reservationName, poolId);
                return true;
            }

            void CollectGarbage()
            {
                var toCollect = Databases.Where(db => db.ShouldBeReleased).OrderBy(db => db.ReservationDate).Take(30).ToList();

                foreach(var database in toCollect)
                {
                    if(database.IsReserved)
                    {
                        database.Release();
                    }
                    database.Reserve("Garbage_collection_task", Guid.NewGuid());
                }

                toCollect.ForEach(db => Release(db.Id).Clean());
            }

            internal void ReleaseReservationsFor(Guid poolId) { DatabasesReservedBy(poolId).ForEach(db => db.Release()); }

            internal IReadOnlyList<Database> DatabasesReservedBy(Guid poolId) => _databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId)
                                                                                           .ToList();

            internal Database Insert()
            {
                var database = new Database(_databases.Count + 1);
                _databases.Add(database);
                return database;
            }

            Database Get(int id) => _databases.Single(db => db.Id == id);

            internal void Reset() { _databases.Clear(); }

            public void Deserialize(BinaryReader reader)
            {
                while(reader.ReadBoolean()) //I use negative boolean to mark end of object
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
                writer.Write(false); //use false to mark end of graph
            }
        }
    }
}
