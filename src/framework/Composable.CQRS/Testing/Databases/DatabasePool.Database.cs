﻿using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.Serialization;
using Composable.SystemCE;
using JetBrains.Annotations;

namespace Composable.Testing.Databases
{
    partial class DatabasePool
    {
        internal class Database : BinarySerialized<Database>
        {
            protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[]
                                                                                        {
                                                                                            GetterSetter.ForInt32(@this => @this.Id, (@this, value) => @this.Id = value),
                                                                                            GetterSetter.ForBoolean(@this => @this.IsReserved, (@this, value) => @this.IsReserved = value),
                                                                                            GetterSetter.ForDateTime(@this => @this.ReservationExpirationTime, (@this, value) => @this.ReservationExpirationTime = value),
                                                                                            GetterSetter.ForString(@this => @this.ReservationName, (@this, value) => @this.ReservationName = value),
                                                                                            GetterSetter.ForGuid(@this => @this.ReservedByPoolId, (@this, value) => @this.ReservedByPoolId = value),
                                                                                            GetterSetter.ForBoolean(@this => @this.IsClean, (@this, value) => @this.IsClean = value)
                                                                                        };

            internal int Id { get; private set; }
            internal bool IsReserved { get; private set; }
            internal bool IsClean { get; private set; } = true;
            public DateTime ReservationExpirationTime { get; private set; } = DateTime.MinValue;
            internal string ReservationName { get; private set; } = string.Empty;
            internal Guid ReservedByPoolId { get; private set; } = Guid.Empty;

            internal string Name => $"{PoolDatabaseNamePrefix}{Id:0000}";

            [UsedImplicitly]public Database() { }
            internal Database(int id) => Id = id;
            internal Database(string name) : this(IdFromName(name)) { }

            internal bool ShouldBeReleased => IsReserved && ReservationExpirationTime < DateTime.UtcNow;
            internal bool IsFree => !IsReserved;

            static int IdFromName(string name)
            {
                var nameIndex = name.ReplaceInvariant(PoolDatabaseNamePrefix, "");
                return IntCE.ParseInvariant(nameIndex);
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
                ReservationExpirationTime = DateTime.UtcNow + reservationLength;
                ReservedByPoolId = poolId;
                return this;
            }

            public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(IsReserved)}: {IsReserved}, {nameof(IsClean)}: {IsClean}, {nameof(ReservationExpirationTime)}: {ReservationExpirationTime}, {nameof(ReservationName)}:{ReservationName}, {nameof(ReservedByPoolId)}:{ReservedByPoolId}";
        }
    }
}