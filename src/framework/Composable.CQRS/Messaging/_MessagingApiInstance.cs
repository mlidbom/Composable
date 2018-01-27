// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class MessagingApiInstance
    {
        public LocalApiInstance Local => new LocalApiInstance();
        public LocalApiInstance Remote => new LocalApiInstance();

        public partial class LocalApiInstance
        {

        }

        public partial class RemoteApiInstance
        {
            public NonTransactionalApiInstance NonTransactional => new NonTransactionalApiInstance();
            public ExactlyOnceApiInstance ExactlyOnce => new ExactlyOnceApiInstance();

            public partial class NonTransactionalApiInstance
            {

            }

            public partial class ExactlyOnceApiInstance
            {

            }
        }
    }
}
