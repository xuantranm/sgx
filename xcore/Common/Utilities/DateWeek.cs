using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Common.Utilities
{
    public static class DateWeek
    {
        /// <summary>Get the week number of a certain date, provided that
        /// the first day of the week is Monday, the first week of a year
        /// is the one that includes the first Thursday of that year and
        /// the last week of a year is the one that immediately precedes
        /// the first calendar week of the next year.
        /// </summary>
        /// <param name="date">Date of interest.</param>
        /// <returns>The week number.</returns>
        public static int GetWeekNumber(this DateTime date)
        {
            //Constants
            const int JAN = 1;
            const int DEC = 12;
            const int LASTDAYOFDEC = 31;
            const int FIRSTDAYOFJAN = 1;
            const int THURSDAY = 4;
            bool thursdayFlag = false;

            //Get the day number since the beginning of the year
            int dayOfYear = date.DayOfYear;

            //Get the first and last weekday of the year
            int startWeekDay = (int)(new DateTime(date.Year, JAN, FIRSTDAYOFJAN)).DayOfWeek;
            int endWeekDay = (int)(new DateTime(date.Year, DEC, LASTDAYOFDEC)).DayOfWeek;

            //Compensate for using monday as the first day of the week
            if (startWeekDay == 0)
            {
                startWeekDay = 7;
            }
            if (endWeekDay == 0)
            {
                endWeekDay = 7;
            }

            //Calculate the number of days in the first week
            int daysInFirstWeek = 8 - (startWeekDay);

            //Year starting and ending on a thursday will have 53 weeks
            if (startWeekDay == THURSDAY || endWeekDay == THURSDAY)
            {
                thursdayFlag = true;
            }

            //We begin by calculating the number of FULL weeks between
            //the year start and our date. The number is rounded up so
            //the smallest possible value is 0.
            int fullWeeks = (int)Math.Ceiling((dayOfYear - (daysInFirstWeek)) / 7.0);
            int result = fullWeeks;

            //If the first week of the year has at least four days, the
            //actual week number for our date can be incremented by one.
            if (daysInFirstWeek >= THURSDAY)
            {
                result = result + 1;
            }

            //If the week number is larger than 52 (and the year doesn't
            //start or end on a thursday), the correct week number is 1.
            if (result > 52 && !thursdayFlag)
            {
                result = 1;
            }

            //If the week number is still 0, it means that we are trying
            //to evaluate the week number for a week that belongs to the
            //previous year (since it has 3 days or less in this year).
            //We therefore execute this function recursively, using the
            //last day of the previous year.
            if (result == 0)
            {
                result = GetWeekNumber(new DateTime(date.Year - 1, DEC, LASTDAYOFDEC));
            }

            return result;
        }

        /// <summary>
        /// Get the first date of the week for a certain date, provided
        /// that the first day of the week is Monday, the first week of
        /// a year is the one that includes the first Thursday of that
        /// year and the last week of a year is the one that immediately
        /// precedes the first calendar week of the next year.
        /// </summary>
        /// <param name="date">ISO 8601 date of interest.</param>
        /// <returns>The first week date.</returns>
        public static DateTime GetFirstDateOfWeek(this DateTime date)
        {
            if (date == DateTime.MinValue)
            {
                return date;
            }

            int week = date.GetWeekNumber();

            while (week == date.GetWeekNumber())
            {
                date = date.AddDays(-1);
            }

            return date.AddDays(1);
        }

        public static DateTime FirstDateOfWeek(int year, int weekOfYear, System.Globalization.CultureInfo ci)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            DateTime firstWeekDay = jan1.AddDays(daysOffset);
            int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            if ((firstWeek <= 1 || firstWeek >= 52) && daysOffset >= -3)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }

        /// <summary>
        /// Get the last date of the week for a certain date, provided
        /// that the first day of the week is Monday, the first week of
        /// a year is the one that includes the first Thursday of that
        /// year and the last week of a year is the one that immediately
        /// precedes the first calendar week of the next year.
        /// </summary>
        /// <param name="date">ISO 8601 date of interest.</param>
        /// <returns>The first week date.</returns>
        public static DateTime GetLastDateOfWeek(this DateTime date)
        {
            if (date == DateTime.MaxValue)
            {
                return date;
            }

            int week = date.GetWeekNumber();

            while (week == date.GetWeekNumber())
            {
                date = date.AddDays(1);
            }

            return date.AddDays(-1);
        }

        public static List<int> Weeks(DateTime start, DateTime end)
        {
            List<int> weeks = new List<int>();
            var Week = (int)Math.Floor((double)start.DayOfYear / 7.0); //starting week number
            for (DateTime t = start; t < end; t = t.AddDays(7))
            {
                weeks.Add(Week);
                Week++;
            }
            return weeks;
        }

        public static int GetWeeksInYear(int year)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(year, 12, 31);
            Calendar cal = dfi.Calendar;
            return cal.GetWeekOfYear(date1, dfi.CalendarWeekRule,
                                                dfi.FirstDayOfWeek);
        }

        #region Other method GetWeeksInYear
        public static int GetIso8601WeekOfYear(this DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static int GetWeeksInGivenYear(int year)
        {
            DateTime lastDate = new DateTime(year, 12, 31);
            int lastWeek = GetIso8601WeekOfYear(lastDate);
            while (lastWeek == 1)
            {
                lastDate = lastDate.AddDays(-1);
                lastWeek = GetIso8601WeekOfYear(lastDate);
            }
            return lastWeek;
        }
        #endregion

        public static int GetWeekNumberOfMonth(DateTime date)
        {
            date = date.Date;
            DateTime firstMonthDay = new DateTime(date.Year, date.Month, 1);
            DateTime firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            if (firstMonthMonday > date)
            {
                firstMonthDay = firstMonthDay.AddMonths(-1);
                firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            }
            return (date - firstMonthMonday).Days / 7 + 1;
        }
    }
}
