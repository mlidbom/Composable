// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public class BusApiInstance
    {
        public LocalApiInstance Local => new LocalApiInstance();
        public LocalApiInstance Remote => new LocalApiInstance();

        public class LocalApiInstance
        {

        }

        public class RemoteApiInstance
        {
            public NonTransactionalApiInstance NonTransactional => new NonTransactionalApiInstance();
            public ExactlyOnceApiInstance ExactlyOnce => new ExactlyOnceApiInstance();

            public class NonTransactionalApiInstance
            {

            }

            public class ExactlyOnceApiInstance
            {

            }
        }
    }
}
