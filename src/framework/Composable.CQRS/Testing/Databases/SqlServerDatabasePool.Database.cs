using System;
using Composable.Contracts;
using Composable.Serialization;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool
    {
        internal class Database : BinarySerializedObject<Database>
        {
            static Database()
            {
                Init(() => new Database(),
                     GetterSetter.ForInt32(@this => @this.Id, (@this, value) => @this.Id = value),
                     GetterSetter.ForBoolean(@this => @this.IsReserved, (@this, value) => @this.IsReserved = value),
                     GetterSetter.ForDateTime(@this => @this.ExpirationDateTime, (@this, value) => @this.ExpirationDateTime = value),
                     GetterSetter.ForString(@this => @this.ReservationName, (@this, value) => @this.ReservationName = value),
                     GetterSetter.ForGuid(@this => @this.ReservedByPoolId, (@this, value) => @this.ReservedByPoolId = value),
                     GetterSetter.ForBoolean(@this => @this.IsClean, (@this, value) => @this.IsClean = value));
            }

            internal int Id { get; private set; }
            internal bool IsReserved { get; private set; }
            internal bool IsClean { get; private set; } = true;
            public DateTime ExpirationDateTime { get; private set; } = DateTime.MinValue;
            internal string ReservationName { get; private set; } = string.Empty;
            internal Guid ReservedByPoolId { get; private set; } = Guid.Empty;

            public Database() { }
            internal Database(int id) => Id = id;
            internal Database(string name) : this(IdFromName(name)) { }

            internal bool ShouldBeReleased => IsReserved && ExpirationDateTime < DateTime.UtcNow;
            internal bool IsFree => !IsReserved;

            static int IdFromName(string name)
            {
                var nameIndex = name.Replace(PoolDatabaseNamePrefix, "");
                return int.Parse(nameIndex);
            }

            internal Database Release()
            {
                Contract.Assert.That(IsReserved, "IsReserved");
                IsReserved = false;
                IsClean = false;
                ReservationName = string.Empty;
                ReservedByPoolId = Guid.Empty;
                return this;
            }

            internal Database Clean()
            {
                Contract.Assert.That(!IsClean, "!IsClean");
                IsClean = true;
                return this;
            }

            internal Database Reserve(string reservationName, Guid poolId, TimeSpan reservationLength)
            {
                Contract.Assert.That(!IsReserved, "!IsReserved");
                Contract.Assert.That(poolId != Guid.Empty, "poolId != Guid.Empty");

                IsReserved = true;
                ReservationName = reservationName;
                ExpirationDateTime = DateTime.UtcNow + reservationLength;
                ReservedByPoolId = poolId;
                return this;
            }

            public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(ExpirationDateTime)}: {ExpirationDateTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
        }
    }
}