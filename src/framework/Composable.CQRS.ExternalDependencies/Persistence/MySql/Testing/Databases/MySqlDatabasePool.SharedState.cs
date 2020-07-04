using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Serialization;
using Composable.System.Linq;
using JetBrains.Annotations;

namespace Composable.Persistence.MySql.Testing.Databases
{
    sealed partial class MySqlDatabasePool
    {
        [UsedImplicitly] class SharedState : BinarySerialized<SharedState>
        {
            protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[] {GetterSetter.ForBinarySerializableList(@this => @this._databases, (@this, value) => @this._databases = value)};

            List<Database> _databases = new List<Database>();
            IReadOnlyList<Database> Databases => _databases;

            internal bool IsEmpty => _databases.Count == 0;

            internal bool IsValid
            {
                get
                {
                    if(_databases.Count != 30)
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
            }

            internal bool TryReserve(out Database reserved, string reservationName, Guid poolId, TimeSpan reservationLength)
            {
                CollectGarbage();

                reserved = _databases.Where(db => !db.IsReserved)
                                     .OrderBy(db => db.ExpirationDateTime)
                                     .FirstOrDefault();

                if(reserved == null)
                {
                    return false;
                }

                reserved.Reserve(reservationName, poolId, reservationLength);
                return true;
            }

            void CollectGarbage()
            {
                var toCollect = Databases.Where(db => db.ShouldBeReleased).OrderBy(db => db.ExpirationDateTime).Take(30).ToList();

                foreach(var database in toCollect)
                {
                    if(database.IsReserved)
                    {
                        database.Release();
                    }
                }
            }

            internal void ReleaseReservationsFor(Guid poolId) => DatabasesReservedBy(poolId).ForEach(db => db.Release());

            internal IReadOnlyList<Database> DatabasesReservedBy(Guid poolId) => _databases.Where(db => db.IsReserved && db.ReservedByPoolId == poolId).ToList();

            internal Database Insert()
            {
                var database = new Database(_databases.Count + 1);
                _databases.Add(database);
                return database;
            }

            internal void Reset() { _databases.Clear(); }
        }
    }
}
