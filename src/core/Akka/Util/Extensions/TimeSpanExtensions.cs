//-----------------------------------------------------------------------
// <copyright file="TimeSpanExtensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;

namespace Akka.Util.Extensions
{
    public enum TimeUnit
    {
        Days,
        Hours,
        Minutes,
        Seconds,
        Milliseconds,
        Microseconds,
        Nanoseconds
    }

    /// <summary>
    /// TimeSpanExtensions
    /// </summary>
    public static class TimeSpanExtensions
    {
        private const long NanosPerTick = 100;

        /// <summary>
        /// Multiplies a timespan by an integer value
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, int multiplier)
        {
            return TimeSpan.FromTicks(multiplicand.Ticks * multiplier);
        }

        /// <summary>
        /// Multiplies a timespan by a double value
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));
        }

        public static long TotalNanos(this TimeSpan duration)
            => duration.Ticks * NanosPerTick;

        public static string Pretty(this TimeSpan? duration)
            => duration.Pretty(includeNanos: false);

        public static string Pretty(this TimeSpan? duration, bool includeNanos, int precision = 4)
        {
            if(precision <= 0)
                throw new ArgumentException("precision must be > 0", nameof(precision));

            if (!duration.HasValue)
                return "undefined";

            var val = duration.Value;

            if (val.Equals(Timeout.InfiniteTimeSpan))
                return "∞ (infinity)";

            var (value, unit) = val.ChooseUnit();

            return string.Format(
                "{0} {1}{2}",
                value.ToString($"F{precision}"),
                Abbreviate(unit),
                includeNanos ? $" ({val.TotalNanos()} ns)" : "");
        }

        public static (double, TimeUnit) ChooseUnit(this TimeSpan d)
        {
            var value = d.TotalDays;
            if (value > 1) return (value, TimeUnit.Days);

            value = d.TotalHours;
            if (value > 1) return (value, TimeUnit.Hours);

            value = d.TotalMinutes;
            if (value > 1) return (value, TimeUnit.Minutes);

            value = d.TotalSeconds;
            if (value > 1) return (value, TimeUnit.Seconds);

            return (d.TotalMilliseconds, TimeUnit.Milliseconds);
        }

        public static string Abbreviate(this TimeUnit unit)
        {
            switch (unit)
            {
                case TimeUnit.Days: return "d";
                case TimeUnit.Hours: return "h";
                case TimeUnit.Minutes: return "min";
                case TimeUnit.Seconds: return "s";
                case TimeUnit.Milliseconds: return "ms";
                case TimeUnit.Microseconds: return "μs";
                case TimeUnit.Nanoseconds: return "ns";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }
    }
}
