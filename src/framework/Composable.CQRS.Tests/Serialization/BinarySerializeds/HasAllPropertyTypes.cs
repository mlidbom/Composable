using System;
using System.Collections.Generic;
using Composable.Serialization;

namespace Composable.Tests.Serialization.BinarySerializeds
{
    class HasAllPropertyTypes : BinarySerialized<HasAllPropertyTypes>
    {
        public static HasAllPropertyTypes CreateInstance() => new HasAllPropertyTypes(true, 2, 'a', new decimal(3.2), 4.1, 5, 6, 7, 8, 9, 10, 11.1f, 12, "13", Guid.Parse("00000000-0000-0000-0000-000000000014"), DateTime.FromBinary(15));

        protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[]
                                                                                    {
                                                                                        GetterSetter.ForBoolean(@this => @this.Boolean, (@this, value) => @this.Boolean = value),
                                                                                        GetterSetter.ForByte(@this => @this.Byte, (@this, value) => @this.Byte = value),
                                                                                        GetterSetter.ForChar(@this => @this.Char, (@this, value) => @this.Char = value),
                                                                                        GetterSetter.ForDecimal(@this => @this.Decimal, (@this, value) => @this.Decimal = value),
                                                                                        GetterSetter.ForChar(@this => @this.Char, (@this, value) => @this.Char = value),
                                                                                        GetterSetter.ForDouble(@this => @this.Double, (@this, value) => @this.Double = value),
                                                                                        GetterSetter.ForInt16(@this => @this.Int16, (@this, value) => @this.Int16 = value),
                                                                                        GetterSetter.ForInt32(@this => @this.Int32, (@this, value) => @this.Int32 = value),
                                                                                        GetterSetter.ForInt64(@this => @this.Int64, (@this, value) => @this.Int64 = value),
                                                                                        GetterSetter.ForSByte(@this => @this.SByte, (@this, value) => @this.SByte = value),
                                                                                        GetterSetter.ForSingle(@this => @this.Single, (@this, value) => @this.Single = value),
                                                                                        GetterSetter.ForString(@this => @this.String, (@this, value) => @this.String = value),
                                                                                        GetterSetter.ForUInt16(@this => @this.UInt16, (@this, value) => @this.UInt16 = value),
                                                                                        GetterSetter.ForUInt32(@this => @this.UInt32, (@this, value) => @this.UInt32 = value),
                                                                                        GetterSetter.ForUInt64(@this => @this.UInt64, (@this, value) => @this.UInt64 = value),
                                                                                        GetterSetter.ForDateTime(@this => @this.DateTime, (@this, value) => @this.DateTime = value),
                                                                                        GetterSetter.ForGuid(@this => @this.Guid, (@this, value) => @this.Guid = value),
                                                                                        GetterSetter.ForBinarySerializable(@this => @this.RecursiveProperty, (@this, value) => @this.RecursiveProperty = value),
                                                                                        GetterSetter.ForBinarySerializableList(@this => @this.RecursiveListProperty, (@this, value) => @this.RecursiveListProperty = value),
                                                                                        GetterSetter.ForBinarySerializableArray(@this => @this.RecursiveArrayProperty, (@this, value) => @this.RecursiveArrayProperty = value)
                                                                                    };

        public HasAllPropertyTypes() {}

        public HasAllPropertyTypes(bool boolean, byte b, char c, decimal @decimal, double d, short int16, int int32, long int64, ushort uInt16, uint uInt32, ulong uInt64, float single, sbyte sByte, string s, Guid guid, DateTime dateTime)
        {
            Boolean = boolean;
            Byte = b;
            Char = c;
            Decimal = @decimal;
            Double = d;
            Int16 = int16;
            Int32 = int32;
            Int64 = int64;
            UInt16 = uInt16;
            UInt32 = uInt32;
            UInt64 = uInt64;
            Single = single;
            SByte = sByte;
            String = s;
            Guid = guid;
            DateTime = dateTime;
        }

        public HasAllPropertyTypes RecursiveProperty { get; set; }
        public List<HasAllPropertyTypes> RecursiveListProperty { get; set; }
        public HasAllPropertyTypes[] RecursiveArrayProperty { get; set; }

        public Boolean Boolean { get; set; }
        public Byte Byte { get; set; }
        public char Char { get; set; }
        public Decimal Decimal { get; set; }
        public Double Double { get; set; }
        public Int16 Int16 { get; set; }
        public Int32 Int32 { get; set; }
        public Int64 Int64 { get; set; }
        public UInt16 UInt16 { get; set; }
        public UInt32 UInt32 { get; set; }
        public UInt64 UInt64 { get; set; }
        public Single Single { get; set; }
        public SByte SByte { get; set; }
        public String String { get; set; }
        public Guid Guid { get; set; }
        public DateTime DateTime { get; set; }
    }
}
