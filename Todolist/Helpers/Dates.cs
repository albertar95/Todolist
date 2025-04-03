using System;
using System.Collections.Generic;
using System.Globalization;

namespace Todolist.Helpers
{
    public class Dates
    {
        public static Tuple<DateTime,DateTime> GetWeekPeriod(int DistanceFromNow = 0) 
        {
            int diff = (7 + (DateTime.Now.DayOfWeek - DayOfWeek.Saturday)) % 7;
            DateTime CurrentWeekStart = DateTime.Now.AddDays(-1 * diff);
            return new Tuple<DateTime, DateTime>(CurrentWeekStart.AddDays(7 * DistanceFromNow).Date, CurrentWeekStart.AddDays(7 * DistanceFromNow + 6).Date);
        }
        public static Tuple<string, string> ToPersianDate(Tuple<DateTime, DateTime> dates) 
        {
            PersianCalendar pc = new PersianCalendar();
            return new Tuple<string, string>($"{pc.GetYear(dates.Item1).ToString("0000")}/{pc.GetMonth(dates.Item1).ToString("00")}/{pc.GetDayOfMonth(dates.Item1).ToString("00")}",
                $"{pc.GetYear(dates.Item2).ToString("0000")}/{pc.GetMonth(dates.Item2).ToString("00")}/{pc.GetDayOfMonth(dates.Item2).ToString("00")}");
        }
        public static string ToPersianDate2(DateTime date,bool IncludeTime = false)
        {
            PersianCalendar pc = new PersianCalendar();
            if(!IncludeTime)
                return $"{pc.GetYear(date).ToString("0000")}/{pc.GetMonth(date).ToString("00")}/{pc.GetDayOfMonth(date).ToString("00")}";
            else
                return $"{pc.GetYear(date).ToString("0000")}/{pc.GetMonth(date).ToString("00")}/{pc.GetDayOfMonth(date).ToString("00")} {date.Hour.ToString("00")}:{date.Minute.ToString("00")}:{date.Second.ToString("00")}";
        }
        public static int CurrentMonth()
        {
            PersianCalendar pc = new PersianCalendar();
            return pc.GetMonth(DateTime.Now);
        }
        public static Tuple<DateTime, DateTime> GetStartAndEndOfMonth(int month,int year = 1404)
        {
            PersianCalendar pc = new PersianCalendar();
            var currentYear = year;
            var StartOfMonth = pc.ToDateTime(currentYear, month, 1, 0, 0, 0, 0);
            var EndOfMonthDay = 30;
            if (month == 12 && !pc.IsLeapYear(currentYear))
                EndOfMonthDay = 29;
            if (month >= 1 && month <= 6)
                EndOfMonthDay = 31;
            var EndOfMonth = pc.ToDateTime(currentYear, month, EndOfMonthDay, 23, 59, 59, 999);
            return new Tuple<DateTime, DateTime>(StartOfMonth,EndOfMonth);
        }
        public static Tuple<DateTime,DateTime> GetStartAndEndOfYear(int year = 1404)
        {
            PersianCalendar pc = new PersianCalendar();
            return new Tuple<DateTime, DateTime>(pc.ToDateTime(year, 1, 1, 0, 0, 0, 0), pc.ToDateTime(year, 12, 29, 23, 59, 59, 999));
        }
        public static List<Tuple<DateTime,DateTime>> GetMonthsOfYear(int year = 1404)
        {
            var result = new List<Tuple<DateTime, DateTime>>();
            int currentMonth = year == 1404 ? CurrentMonth() : 12;
            for (int i = 1; i <= currentMonth; i++)
            {
                result.Add(GetStartAndEndOfMonth(i,year));
            }
            return result;
        }
    }
}
