namespace Void.Time.Impl
{
    public class SimpleTimePositioned : ITimePositioned
    {
        public ITimePoint TimeCoordinate { get; private set; }

        public SimpleTimePositioned(ITimePoint position)
        {
            TimeCoordinate = position.TimeCoordinate;
        }
    }
}