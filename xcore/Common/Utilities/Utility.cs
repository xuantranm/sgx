using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Common.Enums;
using System.Text;
using System.Net;
using System.Globalization;
using System.Collections.Generic;
using Data;
using MongoDB.Driver;
using Models;
//using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Common.Utilities
{
    public static class Utility
    {
        private static MongoDBContext dbContext = new MongoDBContext();

        static Utility()
        {
        }

        #region Rights
        public static bool IsRight(string userId, string role, int action)
        {
            #region Filter
            var builder = Builders<RoleUser>.Filter;
            var filter = builder.Eq(m => m.User, userId);
            filter = filter & builder.Eq(m => m.Role, role);
            filter = filter & builder.Gte(m => m.Action, action);
            #endregion

            var item = dbContext.RoleUsers.Find(filter).FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            return true;
        }
        #endregion

        private static readonly Random random = new Random((int)DateTime.Now.Ticks);
        private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const string CharsNoO0 = "abcdefghijklmnpqrstuvwxyz123456789";

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Random(int length)
        {
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }

            return new String(stringChars);
        }

        //Generate RandomNo
        public static int GenerateRandomNo()
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }
        /// <summary>
        /// Generates a random string without 'o' and '0'
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string RandomNoO0(int length)
        {
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = CharsNoO0[random.Next(CharsNoO0.Length)];
            }

            return new String(stringChars);
        }

        public static string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }

        public static string NonUnicode(string text)
        {
            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                                            "đ",
                                            "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
                                            "í","ì","ỉ","ĩ","ị",
                                            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
                                            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
                                            "ý","ỳ","ỷ","ỹ","ỵ",};
            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                                            "d",
                                            "e","e","e","e","e","e","e","e","e","e","e",
                                            "i","i","i","i","i",
                                            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
                                            "u","u","u","u","u","u","u","u","u","u","u",
                                            "y","y","y","y","y",};
            for (int i = 0; i < arr1.Length; i++)
            {
                //text = text.Replace(arr1[i], arr2[i]);
                text = text.Replace(arr1[i], arr2[i]);
            }
            return text;
        }

        public static string EmailConvert(string text)
        {
            text = NonUnicode(text).ToLower().Trim();
            var inputs = text.Split(new string[] { " ", "-" },
                            StringSplitOptions.RemoveEmptyEntries).ToList();
            var last = inputs.Last();
            inputs.RemoveAt(inputs.Count - 1);
            var output = ".";
            foreach (var item in inputs)
            {
                output += item[0];
            }
            return last + output + Constants.MailExtension;
        }

        public static string ReadTextFile(string filePath)
        {
            try
            {
                using (var sr = new StreamReader(filePath))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets plain text from html text
        /// </summary>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public static string HtmlToPlainText(string htmlText)
        {
            return string.IsNullOrWhiteSpace(htmlText) ? string.Empty : Regex.Replace(htmlText, "<[^>]*>", string.Empty);
        }

        /// <summary>
        /// </summary>
        /// <param name="content"></param>
        /// <param name="type">1)field 2)condition 3)prompt  </param>
        /// <returns>
        /// if no regexToMatch return NULL
        /// </returns>
        public static Hashtable FillAllReplacableFields(string content, string type)
        {
            Hashtable hstReplacableFields = null;
            hstReplacableFields = new Hashtable();
            //m_ListSeqOrder = new List<string>();
            //regexToMatch = "<" + type + ">[a-zA-Z0-9\\s<>/',:=-]+?" + "</" + type + ">"; //[field]Applicant Name[/field]
            var regexToMatch = @"\[" + type + @"\][^#]+?" + @"\[/" + type + @"\]";

            var startIndex = type.Length + 2;
            if (regexToMatch == string.Empty) return hstReplacableFields;
            foreach (Match match in Regex.Matches(content, regexToMatch))
            {
                if (!hstReplacableFields.Contains(match.ToString()))
                {
                    int endIndex = match.ToString().Length - (type.Length + 3);
                    string result = match.ToString().Substring(startIndex, endIndex - startIndex);
                    hstReplacableFields.Add(match.ToString(), result); //key :[field]Applicant Name[/field]  value: Applicant name
                    //m_ListSeqOrder.Add(match.ToString());
                }
            }
            return hstReplacableFields;
        }

        /// <summary>
        /// This function returns the Calendar year on basis of month and year.
        /// </summary>
        public static int GetCalendarYearFromAcademicCycle(int year, int month)
        {
            int resultYear = 0;
            switch ((EMonths)month)
            {
                case EMonths.January:
                case EMonths.February:
                case EMonths.March:
                case EMonths.April:
                case EMonths.May:
                case EMonths.June:
                case EMonths.July:
                    resultYear = year + 1;
                    break;
                case EMonths.August:
                case EMonths.September:
                case EMonths.October:
                case EMonths.November:
                case EMonths.December:
                    resultYear = year;
                    break;
            }
            return resultYear;
        }

        public static string GetMonthStringByMonthNumber(int month)
        {
            switch (month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
                default:
                    return string.Empty;
            }
        }

        public static bool IsDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date) || date == "null" || date.Length < 8) return false;

            try
            {
                Convert.ToDateTime(date);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetQueryInGivenCase(string caseString, string alias)
        {
            string value;
            caseString = caseString.Remove(0, alias.Length);
            if (caseString.ToLower() == caseString)
            {
                value = "LOWER(" + alias + caseString + ")";
            }
            else if (caseString.ToUpper() == caseString)
            {
                value = "UPPER(" + alias + caseString + ")";
            }
            else if (System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(caseString) == caseString)
            {
                value = "dbo.TitleCase(" + alias + caseString + ")";
            }
            else
            {
                //value = "dbo.TitleCase("  + alias + caseString + ")";
                value = alias + caseString;
            }

            return value;
        }

        /// <summary>
        /// This function returns the type of document like doc,pdf,jpg
        /// </summary>
        /// <param name="fileName">GetDocumentType</param>
        /// <returns>string</returns>
        public static string GetDocumentType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains(".")) return string.Empty;
            return Path.GetExtension(fileName).ToUpper();
        }

        public static string TwoToFourLanguage(string text)
        {
            var cultureInfo = new CultureInfo(text);
            return cultureInfo.Name;
        }

        public static string NoUnicodeBlankConvert(string text)
        {
            text = NonUnicode(text).ToLower();

            return text.Replace(" ", "");
        }

        public static string AliasConvert(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            text = text.ToLower();
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            text = regex.Replace(text, " ");
            text = NonUnicode(text);
            text = RemoveSpecialCharacters(text);
            return text;
        }

        //public static string RemoveSpecialCharacters(string str)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (char c in str)
        //    {
        //        if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
        //        {
        //            sb.Append(c);
        //        }
        //    }
        //    return sb.ToString();
        //}

        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^0-9a-zA-Z]+", "-");
            //return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        public static string LinkConvert(string text)
        {
            text = "/" + NonUnicode(text).ToLower() + "/";

            return text.Replace(" ", "-");
        }

        public static string TranslateText(string input, string languagePair)
        {
            try
            {
                if (string.IsNullOrEmpty(languagePair))
                {
                    languagePair = "en";
                }
                string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);

                WebClient webClient = new WebClient
                {
                    Encoding = Encoding.UTF8
                };

                string result = webClient.DownloadString(url);

                result = result.Substring(result.IndexOf("<span title=\"") + "<span title=\"".Length);
                result = result.Substring(result.IndexOf(">") + 1);
                result = result.Substring(0, result.IndexOf("</span>"));
                return result.Trim();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static int BusinessDaysUntil(this DateTime fromDate,
                                    DateTime toDate,
                                    IEnumerable<DateTime> holidays = null)
        {
            int result = 0;

            for (DateTime date = fromDate.Date; date < toDate.Date; date = date.AddDays(1))
                if (!IsHoliday(date, holidays) && !IsSunday(date))
                    result += 1;

            return result;
        }

        public static decimal GetBussinessDaysBetweenTwoDates(DateTime start, DateTime end, TimeSpan workdayStartTime, TimeSpan workdayEndTime, IEnumerable<DateTime> holidays = null)
        {
            if (start > end)
                return -1;

            var startTime = start.TimeOfDay;
            var endTime = end.TimeOfDay;
            // If the start time is before the starting hours, set it to the starting hour.
            if (startTime < workdayStartTime) startTime = workdayStartTime;
            if (endTime > workdayEndTime) endTime = workdayEndTime;

            decimal bd = 0;
            decimal hour = 0;
            // Tính ngày theo giờ. 0.5 day < 4h ; 1 day > 4h
            if (start.Date.CompareTo(end.Date) == 0)
            {
                if (!IsHoliday(start, holidays) && !IsSunday(start))
                {
                    hour = (endTime - startTime).Hours;
                    bd = hour <= 4 ? Convert.ToDecimal(0.5) : 1;
                }
            }
            else
            {
                for (DateTime d = start; d < end; d = d.AddDays(1))
                {
                    if (d.Date.CompareTo(start.Date) == 0)
                    {
                        if (!IsHoliday(d, holidays) && !IsSunday(d))
                        {
                            hour = (workdayEndTime - d.TimeOfDay).Hours;
                            if (hour < 1)
                            {
                                bd += 0;
                            }
                            else
                            {
                                bd += hour <= 4 ? Convert.ToDecimal(0.5) : 1;
                            }
                        }
                    }
                    else if (d.Date.CompareTo(end.Date) == 0)
                    {
                        if (!IsHoliday(d, holidays) && !IsSunday(d))
                        {
                            hour = (endTime - workdayStartTime).Hours;
                            if (hour < 1)
                            {
                                bd += 0;
                            }
                            else
                            {
                                bd += hour <= 4 ? Convert.ToDecimal(0.5) : 1;
                            }
                        }
                    }
                    else
                    {
                        if (!IsHoliday(d, holidays) && !IsSunday(d))
                        {
                            ++bd;
                        }
                    }

                    // update start to start Working hour
                    // TimeSpan ts = new TimeSpan(10, 30, 0);
                    d = d.Date + workdayStartTime;
                }
            }

            return bd;
        }

        public static decimal GetHolidaysBetweenTwoDates(DateTime start, DateTime end, TimeSpan workdayStartTime, TimeSpan workdayEndTime, IEnumerable<DateTime> holidays = null)
        {
            if (start > end)
                return -1;

            var startTime = start.TimeOfDay;
            var endTime = end.TimeOfDay;
            // If the start time is before the starting hours, set it to the starting hour.
            if (startTime < workdayStartTime) startTime = workdayStartTime;
            if (endTime > workdayEndTime) endTime = workdayEndTime;

            decimal bd = 0;
            decimal hour = 0;
            // Tính ngày theo giờ. 0.5 day < 4h ; 1 day > 4h
            if (start.Date.CompareTo(end.Date) == 0)
            {
                if (IsHoliday(start, holidays))
                {
                    hour = (endTime - startTime).Hours;
                    bd = hour < 4 ? Convert.ToDecimal(0.5) : 1;
                }
            }
            else
            {
                for (DateTime d = start; d < end; d = d.AddDays(1))
                {
                    if (d.Date.CompareTo(start.Date) == 0)
                    {
                        if (IsHoliday(d, holidays))
                        {
                            hour = (workdayEndTime - d.TimeOfDay).Hours;
                            if (hour < 1)
                            {
                                bd += 0;
                            }
                            else
                            {
                                bd += hour <= 4 ? Convert.ToDecimal(0.5) : 1;
                            }
                        }
                    }
                    else if (d.Date.CompareTo(end.Date) == 0)
                    {
                        if (IsHoliday(start, holidays))
                        {
                            hour = (endTime - workdayStartTime).Hours;
                            if (hour < 1)
                            {
                                bd += 0;
                            }
                            else
                            {
                                bd += hour <= 4 ? Convert.ToDecimal(0.5) : 1;
                            }
                        }
                    }
                    else
                    {
                        if (IsHoliday(d, holidays))
                        {
                            ++bd;
                        }
                    }

                    // update start to start Working hour
                    // TimeSpan ts = new TimeSpan(10, 30, 0);
                    d = d.Date + workdayStartTime;
                }
            }

            return bd;
        }

        private static Boolean IsSunday(DateTime value)
        {
            return value.DayOfWeek == DayOfWeek.Sunday;
        }

        private static Boolean IsHoliday(DateTime value, IEnumerable<DateTime> holidays = null)
        {
            if (null == holidays)
                holidays = VietNamHolidays;

            return holidays.Any(holiday => holiday.Day == value.Day &&
                                            holiday.Month == value.Month);
        }

        private static Boolean IsHolidaySunday(DateTime value, IEnumerable<DateTime> holidays = null)
        {
            if (null == holidays)
                holidays = VietNamHolidays;

            return (value.DayOfWeek == DayOfWeek.Sunday) ||
                    //(value.DayOfWeek == DayOfWeek.Saturday) ||
                    holidays.Any(holiday => holiday.Day == value.Day &&
                                            holiday.Month == value.Month);
        }

        private static readonly List<DateTime> VietNamHolidays = new List<DateTime>() {
          new DateTime(1, 1, 1), //New Year Day
          new DateTime(1, 4, 25), //Dia da Liberdade (PT)
          new DateTime(1, 5, 1), //Labour Day
          new DateTime(1, 6, 10), //Dia de Portugal (PT)
          new DateTime(1, 8, 15), //Assumption of Mary
          new DateTime(1, 10, 5), //Implantação da república (PT)
          new DateTime(1, 11, 1), //All Saints' Day
          new DateTime(1, 12, 1), //Restauração da independência (PT)
          new DateTime(1, 12, 8), //Imaculada Conceição (PT?)
          new DateTime(1, 12, 25), //Christmas
        };

        public static int GetDaysUntilBirthday(DateTime birthday)
        {
            var nextBirthday = birthday.AddYears(DateTime.Today.Year - birthday.Year);
            if (nextBirthday < DateTime.Today)
            {
                nextBirthday = nextBirthday.AddYears(1);
            }
            return (nextBirthday - DateTime.Today).Days;
        }

        public static string GetMonthsDaysUntilBirthday(DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int months = 0;
            int days = 0;

            DateTime nextBirthday = birthday.AddYears(today.Year - birthday.Year);
            if (nextBirthday < today)
            {
                nextBirthday = nextBirthday.AddYears(1);
            }

            while (today.AddMonths(months + 1) <= nextBirthday)
            {
                months++;
            }
            days = nextBirthday.Subtract(today.AddMonths(months)).Days;

            return string.Format("Next birthday is in {0} month(s) and {1} day(s).", months, days);
        }

        public static DateTime WorkingMonthToDate(string times)
        {
            if (string.IsNullOrEmpty(times))
            {
                // calculator date: 26 - > 25
                // now: 25/08 => [to] times: -> 25/07
                // now: 26/08 => [to] times: -> 25/08
                // now: 01/09 => [to] times: -> 25/08
                // now: 24/09 => [to] times: -> 25/08
                var now = DateTime.Now;
                times = now.Month + "-" + now.Year;
                if (now.Day < 26)
                {
                    var lastMonth = now.AddMonths(-1);
                    times = lastMonth.Month + "-" + lastMonth.Year;
                }
            }
            int month = Convert.ToInt32(times.Split("-")[0]);
            int year = Convert.ToInt32(times.Split("-")[1]);
            return new DateTime(year, month, 25);
        }

        public static string TruncateLongString(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength)) + "...";
        }

        public static int ClosestTo(this IEnumerable<int> collection, int target)
        {
            // NB Method will return int.MaxValue for a sequence containing no elements.
            // Apply any defensive coding here as necessary.
            var closest = int.MaxValue;
            var minDifference = int.MaxValue;
            foreach (var element in collection)
            {
                var difference = Math.Abs((long)element - target);
                if (minDifference > difference)
                {
                    minDifference = (int)difference;
                    closest = element;
                }
            }

            return closest;
        }

        public static int GetIso8601WeekOfYear(DateTime time)
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

        #region FACTORY
        public static string NoPhieuInCa(DateTime date, string xe)
        {
            if (string.IsNullOrEmpty(xe))
            {
                return "";
            }

            int month = date.Month;
            int year = date.Year;
            // check db if exist date vs xe
            var phieu = dbContext.FactoryVanHanhs.Find(m => m.XeCoGioiMayAlias.Equals(xe) && m.Date.Equals(date.Date)).FirstOrDefault();
            if (phieu != null)
            {
                return phieu.PhieuInCa;
            }
            // [no] increase by month.
            if (dbContext.FactoryVanHanhs.CountDocuments(m => m.Month.Equals(month)) > 0)
            {
                var max = dbContext.FactoryVanHanhs.Find(m => m.Year.Equals(year) && m.Month.Equals(month) && !m.PhieuInCa.Equals("")).SortByDescending(m => m.CreatedOn).First();
                return year +  month.ToString("D2") + "-" + (Convert.ToInt32(max.PhieuInCa.Split("-")[1]) + 1).ToString("D4");
            }
            return year + month.ToString("D2") + "-" + 1.ToString("D4");
        }
        #endregion
    }
}