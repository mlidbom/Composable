using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Composable.System;
using JetBrains.Annotations;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        [UsedImplicitly] class SharedState : IBinarySerializeMySelf
        {
            readonly List<Database> _databases = new List<Database>();
            internal IReadOnlyList<Database> Databases => _databases;

            internal Database Release(int id)
            {
                var database = Get(id);
                database.Release();
                return database;
            }

            internal Database Clean(int id)
            {
                var database = Get(id);
                database.Clean();
                return database;
            }

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
                return true;
                
            }

            internal bool TryReserve(out Database reserved, string reservationName, Guid poolId)
            {
                var unreserved = _databases.Where(db => !db.IsReserved)
                                           .OrderBy(db => db.ReservationDate)
                                           .ToList();

                reserved = unreserved.FirstOrDefault(db => db.IsClean);
                if(reserved == null)
                {
                    reserved = unreserved.FirstOrDefault();
                    if(reserved == null)
                    {
                        return false;
                    }
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

            internal IReadOnlyList<Database> ShouldBeReleased() => _databases
                .Where(db => db.ShouldBeReleased).ToList();

            internal IReadOnlyList<Database> ShouldBeCleaned() => _databases
                .Where(db => db.ShouldBeCleaned)
                .OrderBy(db => db.ReservationDate)
                .ToList();

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

    interface IBinarySerializeMySelf
    {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    }
}
