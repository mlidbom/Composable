#region usings

using System;

#endregion

namespace Composable.SpaceTime.Time
{
    /// <summary>A point on a timeline.</summary>
    public interface ITimePoint : ITimeProjectable<ITimePoint>
    {
        /// <summary>The value of this <see cref="ITimePoint"/> represented as a <see cref="DateTime"/></summary>
        DateTime AsDateTime();
    }

    /// <summary>A vector on a timeline. May be positive(forwards in time) or negative(backwards in time).</summary>
    public interface ITimeMovement
    {
        /// <summary>The value of this <see cref="ITimeMovement"/> represented as an <see cref="TimeSpan"/></summary>
        TimeSpan AsTimeSpan();
    }

    /// <summary> A <see cref="ITimeMovement"/> that must be positive. Represents a stretch of time.</summary>
    public interface IDuration : ITimeMovement
    {
    }

    /// <summary>An object positioned on a timeline.</summary>
    public interface ITimePositioned
    {
        /// <summary>The start position of the object on the timeline.</summary>
        ITimePoint TimePosition { get; }
    }

    /// <summary>A segment of a timeline.</summary>
    public interface ITimeInterval : ITimePositioned, IDuration
    {
    }

    /// <summary>A type capable of projecting a clone of itself to a different position in time.</summary>
    /// <typeparam name="TProjection">The type of the projected clone.</typeparam>
    public interface ITimeProjectable<TProjection> : ITimePositioned where TProjection : ITimeProjectable<TProjection>
    {
        /// <summary>Returns a clone of the object projected at the specified position in time.</summary>
        TProjection ProjectAt(ITimePoint targetTime);
    }
}