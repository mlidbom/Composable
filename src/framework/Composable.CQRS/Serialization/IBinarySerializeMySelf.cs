using System.IO;

namespace Composable.Serialization
{
    interface IBinarySerializeMySelf
    {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    }

    interface IBinarySerializeMySelf<TInheritor> : IBinarySerializeMySelf where TInheritor : new()
    {

    }
}