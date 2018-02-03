using System;
using System.IO;

namespace Composable.Tests.System.Threading
{
    abstract partial class BinarySerializedObject<TInheritor> where TInheritor : BinarySerializedObject<TInheritor>, new()
    {
        protected static class GetterSetterFor
        {
            internal static MemberGetterSetter Boolean(Func<TInheritor, Boolean> getter, Action<TInheritor, Boolean> setter) => new BooleanGetterSetter(getter, setter);
            class BooleanGetterSetter : MemberGetterSetter<bool>
            {
                public BooleanGetterSetter(Func<TInheritor, Boolean> getter, Action<TInheritor, Boolean> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadBoolean());
            }

            internal static MemberGetterSetter Byte(Func<TInheritor, Byte> getter, Action<TInheritor, Byte> setter) => new ByteGetterSetter(getter, setter);
            class ByteGetterSetter : MemberGetterSetter<byte>
            {
                public ByteGetterSetter(Func<TInheritor, Byte> getter, Action<TInheritor, Byte> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadByte());
            }

            internal static MemberGetterSetter Char(Func<TInheritor, Char> getter, Action<TInheritor, Char> setter) => new CharGetterSetter(getter, setter);
            class CharGetterSetter : MemberGetterSetter<char>
            {
                public CharGetterSetter(Func<TInheritor, Char> getter, Action<TInheritor, Char> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadChar());
            }

            internal static MemberGetterSetter Decimal(Func<TInheritor, Decimal> getter, Action<TInheritor, Decimal> setter) => new DecimalGetterSetter(getter, setter);
            class DecimalGetterSetter : MemberGetterSetter<decimal>
            {
                public DecimalGetterSetter(Func<TInheritor, Decimal> getter, Action<TInheritor, Decimal> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadDecimal());
            }

            internal static MemberGetterSetter Double(Func<TInheritor, Double> getter, Action<TInheritor, Double> setter) => new DoubleGetterSetter(getter, setter);
            class DoubleGetterSetter : MemberGetterSetter<double>
            {
                public DoubleGetterSetter(Func<TInheritor, Double> getter, Action<TInheritor, Double> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadDouble());
            }

            internal static MemberGetterSetter Int16(Func<TInheritor, Int16> getter, Action<TInheritor, Int16> setter) => new Int16GetterSetter(getter, setter);
            class Int16GetterSetter : MemberGetterSetter<short>
            {
                public Int16GetterSetter(Func<TInheritor, Int16> getter, Action<TInheritor, Int16> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt16());
            }

            internal static MemberGetterSetter Int32(Func<TInheritor, Int32> getter, Action<TInheritor, Int32> setter) => new Int32GetterSetter(getter, setter);
            class Int32GetterSetter : MemberGetterSetter<int>
            {
                public Int32GetterSetter(Func<TInheritor, Int32> getter, Action<TInheritor, Int32> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt32());
            }

            internal static MemberGetterSetter Int64(Func<TInheritor, Int64> getter, Action<TInheritor, Int64> setter) => new Int64GetterSetter(getter, setter);
            class Int64GetterSetter : MemberGetterSetter<long>
            {
                public Int64GetterSetter(Func<TInheritor, Int64> getter, Action<TInheritor, Int64> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt64());
            }

            internal static MemberGetterSetter SByte(Func<TInheritor, SByte> getter, Action<TInheritor, SByte> setter) => new SByteGetterSetter(getter, setter);
            class SByteGetterSetter : MemberGetterSetter<sbyte>
            {
                public SByteGetterSetter(Func<TInheritor, SByte> getter, Action<TInheritor, SByte> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadSByte());
            }

            internal static MemberGetterSetter Single(Func<TInheritor, Single> getter, Action<TInheritor, Single> setter) => new SingleGetterSetter(getter, setter);
            class SingleGetterSetter : MemberGetterSetter<float>
            {
                public SingleGetterSetter(Func<TInheritor, Single> getter, Action<TInheritor, Single> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadSingle());
            }

            internal static MemberGetterSetter String(Func<TInheritor, String> getter, Action<TInheritor, String> setter) => new StringGetterSetter(getter, setter);
            class StringGetterSetter : MemberGetterSetter<string>
            {
                public StringGetterSetter(Func<TInheritor, String> getter, Action<TInheritor, String> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadString());
            }

            internal static MemberGetterSetter UInt16(Func<TInheritor, UInt16> getter, Action<TInheritor, UInt16> setter) => new UInt16GetterSetter(getter, setter);
            class UInt16GetterSetter : MemberGetterSetter<ushort>
            {
                public UInt16GetterSetter(Func<TInheritor, UInt16> getter, Action<TInheritor, UInt16> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt16());
            }

            internal static MemberGetterSetter UInt32(Func<TInheritor, UInt32> getter, Action<TInheritor, UInt32> setter) => new UInt32GetterSetter(getter, setter);
            class UInt32GetterSetter : MemberGetterSetter<uint>
            {
                public UInt32GetterSetter(Func<TInheritor, UInt32> getter, Action<TInheritor, UInt32> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt32());
            }

            internal static MemberGetterSetter UInt64(Func<TInheritor, UInt64> getter, Action<TInheritor, UInt64> setter) => new UInt64GetterSetter(getter, setter);
            class UInt64GetterSetter : MemberGetterSetter<ulong>
            {
                public UInt64GetterSetter(Func<TInheritor, UInt64> getter, Action<TInheritor, UInt64> setter) : base(getter, setter) {}

                internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
                internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt64());
            }
        }
    }
}
