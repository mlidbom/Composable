
namespace Composable.SystemCE
{
    //Performance: Serializer should take advantage of this. No serialized data. Delegating to the Instance property when deserializing. Many API start pages and navigation style pages could implement this and gain a nice performance boost.
    interface IStaticInstancePropertySingleton {}
}
