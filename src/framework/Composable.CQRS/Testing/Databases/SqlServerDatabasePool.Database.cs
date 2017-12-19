using System;
using System.IO;
using Composable.Contracts;
using Composable.System.Threading;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool
    {
        [Serializable]
        internal class Database : IBinarySerializeMySelf
        {
            internal int Id { get; private set; }
            internal bool IsReserved { get; private set; }
            internal bool IsClean { get; private set; } = true;
            public DateTime ExpirationDateTime { get; private set; } = DateTime.MinValue;
            internal string ReservationName { get; private set; } = string.Empty;
            internal Guid ReservedByPoolId { get; private set; } = Guid.Empty;

            internal Database() { }
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
                OldContract.Assert.That(IsReserved, "IsReserved");
                IsReserved = false;
                IsClean = false;
                ReservationName = string.Empty;
                ReservedByPoolId = Guid.Empty;
                return this;
            }

            internal Database Clean()
            {
                OldContract.Assert.That(!IsClean, "!IsClean");
                IsClean = true;
                return this;
            }

            internal Database Reserve(string reservationName, Guid poolId, TimeSpan reservationLength)
            {
                OldContract.Assert.That(!IsReserved, "!IsReserved");
                OldContract.Assert.That(poolId != Guid.Empty, "poolId != Guid.Empty");

                IsReserved = true;
                ReservationName = reservationName;
                ExpirationDateTime = DateTime.UtcNow + reservationLength;
                ReservedByPoolId = poolId;
                return this;
            }

            public void Deserialize(BinaryReader reader)
            {
                Id = reader.ReadInt32();
                IsReserved = reader.ReadBoolean();
                ExpirationDateTime = DateTime.FromBinary(reader.ReadInt64());
                ReservationName = reader.ReadString();
                ReservedByPoolId = new Guid(reader.ReadBytes(16));
                IsClean = reader.ReadBoolean();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(IsReserved);
                writer.Write(ExpirationDateTime.ToBinary());
                writer.Write(ReservationName);
                writer.Write(ReservedByPoolId.ToByteArray());
                writer.Write(IsClean);
            }

            public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(ExpirationDateTime)}: {ExpirationDateTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
        }
    }
}