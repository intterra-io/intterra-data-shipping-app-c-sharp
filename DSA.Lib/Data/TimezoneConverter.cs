using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Data
{
    public class TimezoneConverter
    {
        public static TimeZoneInfo PosixToTimezone(string posix)
        {
            switch (posix?.ToLower())
            {
                case "est":
                case "est7edt":
                    return TimeZoneInfo.FindSystemTimeZoneById("eastern standard time");
                case "cst":
                case "cst7cdt":
                    return TimeZoneInfo.FindSystemTimeZoneById("central standard time");
                case "mst":
                case "mst7mdt":
                    return TimeZoneInfo.FindSystemTimeZoneById("mountain standard time");
                case "pst":
                case "pst7pdt":
                    return TimeZoneInfo.FindSystemTimeZoneById("pacific standard time");
                default:
                    return null;
            }
        }
    }
}
