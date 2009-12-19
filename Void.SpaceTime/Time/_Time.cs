using System;

namespace Void.Time
{
    /// <summary>An object positioned on a timeline.</summary>
    public interface ITimePositioned
    {
        /// <summary>The start position of the object on the timeline.</summary>
        ITimePoint TimePosition { get; }
    }

    /// <summary>An <see cref="ITimePositioned"/> guaranteed to have zero duration.</summary>
    public interface ITimePoint
    {
        DateTime DateTimeValue { get; }
    }

    /// <summary> A length of time. Negative values are illegal.</summary>
    public interface IDuration
    {
        /// <summary>The length of the object on a timeline.</summary>
        TimeSpan Duration { get; }
    }

    /// <summary>A movement on a timeline. May be positive(forwards in time) or negative(backwards in time).</summary>
    public interface ITimeMovement
    {
        TimeSpan AsTimeSpan();
    }

    /// <summary>A segment of a timeline.</summary>
    public interface ITimeInterval : ITimePositioned, IDuration
    {
    }

    /// <summary>A type capable of projecting a clone of itself to a different position in time.</summary>
    public interface ITimeProjectable<T> : ITimePositioned where T : ITimeProjectable<T>
    {
        T ProjectAt(ITimePoint targetTime);
    }
}