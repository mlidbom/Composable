using System.IO;

namespace Composable.Testing.System.Threading
{
    interface IBinarySerializeMySelf
    {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    }
}