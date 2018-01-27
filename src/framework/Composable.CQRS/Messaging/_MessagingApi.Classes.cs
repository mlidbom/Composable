using System;
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class MessagingApi
    {
        public partial class Local
        {
        }

        public partial class Remote
        {
            public partial class NonTransactional
            {
            }

            public partial class ExactlyOnce
            {
            }
        }
    }
}
