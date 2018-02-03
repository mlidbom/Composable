using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable ForCanBeConvertedToForeach optimization is important in this file. It is really the whole purpose of it :)

namespace Composable.Serialization
{
    abstract partial class BinarySerialized<TInheritor> where TInheritor : BinarySerialized<TInheritor>
    {
        static readonly MemberGetterSetter[] MemberGetterSetters;
        static readonly MemberGetterSetter[] MemberGetterSettersReversed;
        internal static TInheritor Construct() => (TInheritor)Activator.CreateInstance(typeof(TInheritor), nonPublic: true);

        readonly TInheritor _this;

        protected BinarySerialized() => _this = (TInheritor)this;

        static BinarySerialized()
        {
            var inheritor = Construct();
            MemberGetterSetters = inheritor.CreateGetterSetters().ToArray();
            MemberGetterSettersReversed = MemberGetterSetters.Reverse().ToArray();
        }

        protected abstract IEnumerable<MemberGetterSetter> CreateGetterSetters();

        void Deserialize(BinaryReader reader)
        {
            for(var index = 0; index < MemberGetterSetters.Length; index++)
            {
                MemberGetterSettersReversed[index].Deserialize(_this, reader);
            }
        }

        void Serialize(BinaryWriter writer)
        {
            for(var index = 0; index < MemberGetterSettersReversed.Length; index++)
            {
                MemberGetterSettersReversed[index].Serialize(_this, writer);
            }
        }

        internal byte[] Serialize()
        {
            //Optimization to know the size of the buffer to use from the start so we don't need to reallocate or to use ToArray. We can just return the existing buffer we created.
            using(var stream = new MemoryStream())
            {
                using(var binaryWriter = new BinaryWriter(stream))
                {
                    Serialize(binaryWriter);
                }

                return stream.ToArray();
            }
        }

        internal static TInheritor Deserialize(byte[] data)
        {
            using(var reader = new BinaryReader(new MemoryStream(data)))
            {
                var instance = Construct();
                instance.Deserialize(reader);
                return instance;
            }
        }

        protected abstract class MemberGetterSetter
        {
            internal abstract void Serialize(TInheritor inheritor, BinaryWriter writer);
            internal abstract void Deserialize(TInheritor inheritor, BinaryReader reader);
        }

        protected abstract class MemberGetterSetter<TValue> : MemberGetterSetter
        {
            protected readonly Func<TInheritor, TValue> Getter;
            protected readonly Action<TInheritor, TValue> Setter;
            protected MemberGetterSetter(Func<TInheritor, TValue> getter, Action<TInheritor, TValue> setter)
            {
                Getter = getter;
                Setter = setter;
            }
        }
    }
}
