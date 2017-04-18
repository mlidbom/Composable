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
            internal void Release(string name)
            {
                Get(name).IsReserved = false;
            }

            internal bool IsValid()
            {
                if(Databases.Count == 0)
                {
                    return false;
                }

                for (int i = 1; i <= Databases.Count; i++)
                {
                    if (i != Databases[i - 1].Id)
                    {
                        return false;
                    }
                }
                return true;
            }

            internal bool TryReserve(out Database reserved)
            {
                reserved = Databases.FirstOrDefault(db => db.IsReserved == false);
                if(reserved == null)
                {
                    return false;
                }

                reserved.IsReserved = true;
                reserved.ReservationDate = DateTime.UtcNow;
                return true;
            }

            internal Database Insert()
            {
                var database = new Database(Databases.Count + 1);
                Databases.Add(database);
                return database;
            }

            internal IReadOnlyList<Database> DbsWithOldLocks() => Databases.Where(db => db.ReservationDate < DateTime.UtcNow - 10.Minutes())
                                                                  .ToList();

            Database Get(string name) => Databases.Single(db => db.Name() == name);

            internal List<Database> Databases { get; set; } = new List<Database>();

            public void Deserialize(BinaryReader reader)
            {
                Databases = new List<Database>();
                while(reader.ReadBoolean())//I use negative boolean to mark end of object
                {
                    var database = new Database();
                    database.Deserialize(reader);
                    Databases.Add(database);
                }
            }

            public void Serialize(BinaryWriter writer)
            {
                Databases.ForEach(db =>
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
