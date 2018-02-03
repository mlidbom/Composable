using System;
using System.IO;
using System.Linq;
using Composable.Contracts;
using Composable.System.Threading;

// ReSharper disable ForCanBeConvertedToForeach optimization is important in this file. It is really the whole purpose of it :)

namespace Composable.Tests.System.Threading
{
    abstract partial class BinarySerializedObject<TInheritor> : IBinarySerializeMySelf<TInheritor>
        where TInheritor : BinarySerializedObject<TInheritor>, new()
    {
        static MemberGetterSetter[] _memberGetterSetters;
        static MemberGetterSetter[] _memberGetterSettersReversed;

        readonly TInheritor _this;

        protected BinarySerializedObject() => _this = (TInheritor)this;

        protected static void InitGetterSetters(MemberGetterSetter[] getterSetters)
        {
            Contract.Invariant(() => _memberGetterSetters).Inspect(currentArray => currentArray == null, currentArray => new InvalidOperationException($"You can only call {nameof(InitGetterSetters)} once"));

            _memberGetterSetters = getterSetters.ToArray();
            _memberGetterSettersReversed = getterSetters.Reverse().ToArray();
        }

        public void Deserialize(BinaryReader reader)
        {
            for(var index = 0; index < _memberGetterSetters.Length; index++)
            {
                _memberGetterSettersReversed[index].Deserialize(_this, reader);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            for(int index = 0; index < _memberGetterSettersReversed.Length; index++)
            {
                _memberGetterSettersReversed[index].Serialize(_this, writer);
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
