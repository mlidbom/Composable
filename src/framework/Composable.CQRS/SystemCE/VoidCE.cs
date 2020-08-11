using System;
using System.Threading.Tasks;

namespace Composable.SystemCE
{
    ///<summary>Enables harmonizing on only using methods with return values without loosing the semantic information that their return value is meaningless. Return <see cref="VoidCE.Instance"/> from methods with "no" return value. </summary>
    sealed class VoidCE : IEquatable<VoidCE>, IStaticInstancePropertySingleton
    {
        public static VoidCE Instance { get; } = new VoidCE();
        public static Task<VoidCE> InstanceTask { get; } = Task.FromResult(Instance);

        VoidCE(){}

        public bool Equals(VoidCE? other) => other != null;
        public override bool Equals(object? other) => Equals(other as VoidCE);
        public override int GetHashCode() => 392576489;
        public static bool operator ==(VoidCE? left, VoidCE? right) => Equals(left, right);
        public static bool operator !=(VoidCE? left, VoidCE? right) => !Equals(left, right);
    }
}