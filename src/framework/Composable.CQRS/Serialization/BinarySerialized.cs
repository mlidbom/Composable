using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable ForCanBeConvertedToForeach optimization is important in this file. It is really the whole purpose of it :)

namespace Composable.Serialization
{
    abstract partial class BinarySerialized<TInheritor>
        where TInheritor : BinarySerialized<TInheritor>
    {
        static MemberGetterSetter[] _memberGetterSetters;
        static MemberGetterSetter[] _memberGetterSettersReversed;
        static Func<TInheritor> _constructor;

        readonly TInheritor _this;

        protected BinarySerialized() => _this = (TInheritor)this;

        static BinarySerialized()
        {
            _constructor = () => (TInheritor)Activator.CreateInstance(typeof(TInheritor), nonPublic: true);
            var inheritor = _constructor();
            Init(_constructor, inheritor.CreateGetterSetters().ToArray());
        }

        protected abstract IEnumerable<MemberGetterSetter> CreateGetterSetters();

        protected static void Init(Func<TInheritor> constructor, params MemberGetterSetter[] getterSetters)
        {
            if(_memberGetterSetters != null)
            {
                throw new InvalidOperationException($"You can only call {nameof(Init)} once");
            }

            _constructor = constructor;

            _memberGetterSetters = getterSetters.ToArray();
            _memberGetterSettersReversed = getterSetters.Reverse().ToArray();
        }

        void Deserialize(BinaryReader reader)
        {
            for(var index = 0; index < _memberGetterSetters.Length; index++)
            {
                _memberGetterSettersReversed[index].Deserialize(_this, reader);
            }
        }

        void Serialize(BinaryWriter writer)
        {
            for(var index = 0; index < _memberGetterSettersReversed.Length; index++)
            {
                _memberGetterSettersReversed[index].Serialize(_this, writer);
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
                var instance = _constructor();
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
