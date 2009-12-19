namespace Void.Time.Impl
{
    public class SimpleTimePositioned : ITimePositioned
    {
        public ITimePoint TimePosition { get; private set; }

        public SimpleTimePositioned(ITimePoint position)
        {
            TimePosition = position;
        }
    }
}