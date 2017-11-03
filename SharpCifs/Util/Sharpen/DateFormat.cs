using System;
using System.Globalization;

namespace SharpCifs.Util.Sharpen
{
    internal abstract class DateFormat
    {
        public const int Default = 2;

        private TimeZoneInfo _timeZone;

        public abstract DateTime Parse(string value);

        public TimeZoneInfo GetTimeZone()
        {
            return _timeZone;
        }

        public void SetTimeZone(TimeZoneInfo timeZone)
        {
            this._timeZone = timeZone;
        }

        public abstract string Format(DateTime time);
    }
}