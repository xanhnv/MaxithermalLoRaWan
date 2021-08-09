using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MaxithermalWebApplication
{
    public static class Helper
    {
        private static readonly long UnixEpochTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public static long? ToJsonTicks(this DateTime? value)
        {
            return value == null ? (long?)null : (value.Value.ToUniversalTime().Ticks - UnixEpochTicks) / 10000;
        }

        public static double ToJsonTicks(this DateTime value)
        {
           // return (value.ToUniversalTime().Ticks - UnixEpochTicks) / 10000;
            return(value.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}