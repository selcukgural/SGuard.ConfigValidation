using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SGuard.ConfigValidation.Utilities
{
    /// <summary>
    /// Lightweight non-allocating stopwatch alternative using <see cref="Stopwatch.GetTimestamp"/>.
    /// </summary>
    /// <remarks>
    /// - This is a readonly value-type intended for short-lived, high-frequency measurements.
    /// - It does not allocate on the managed heap and therefore reduces GC pressure in hot paths.
    /// - The type intentionally does not implement IDisposable and does not support Stop/Restart semantics.
    /// - Thread-safety: immutable; safe to copy by value. Avoid relying on identity semantics.
    /// </remarks>
    public readonly struct ValueStopwatch
    {
        private readonly long _startTimestamp;

        /// <summary>
        /// Initializes a new instance of <see cref="ValueStopwatch"/> with a given start timestamp.
        /// </summary>
        /// <param name="startTimestamp">Raw timestamp from <see cref="Stopwatch.GetTimestamp"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueStopwatch(long startTimestamp)
        {
            _startTimestamp = startTimestamp;
        }

        /// <summary>
        /// Starts and returns a new <see cref="ValueStopwatch"/>.
        /// </summary>
        /// <returns>A started <see cref="ValueStopwatch"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueStopwatch StartNew()
        {
            return new ValueStopwatch(Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Gets the raw elapsed timestamp ticks (difference of <see cref="Stopwatch.GetTimestamp"/> values).
        /// </summary>
        public long ElapsedTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Stopwatch.GetTimestamp() - _startTimestamp;
        }

        /// <summary>
        /// Gets the elapsed time in milliseconds. Rounded down to nearest millisecond.
        /// </summary>
        public long ElapsedMilliseconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                long ticks = ElapsedTicks;
                return ticks * 1000L / Stopwatch.Frequency;
            }
        }

        /// <summary>
        /// Gets the elapsed time as a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan Elapsed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TimeSpan.FromSeconds((double)ElapsedTicks / Stopwatch.Frequency);
        }

        /// <summary>
        /// Returns a string representation of the elapsed time for debugging.
        /// </summary>
        /// <returns>Elapsed time as a string.</returns>
        public override string ToString()
        {
            return Elapsed.ToString();
        }
    }
}

