using System;

namespace Void.Time.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    public class SimpleTimePositioned : ITimePositioned
    {
        public ITimePoint TimePosition { get; private set; }

        public SimpleTimePositioned(ITimePoint position)
        {
            TimePosition = position;
        }
    }
}