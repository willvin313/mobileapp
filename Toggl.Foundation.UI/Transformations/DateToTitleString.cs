using System;
using System.Globalization;

namespace Toggl.Foundation.UI.Transformations
{
    public sealed class DateToTitleString
    {
        public static string Convert(DateTimeOffset date, DateTimeOffset now, CultureInfo cultureInfo = null)
        {
            var localDate = date.ToLocalTime().Date;
            var localNow = now.ToLocalTime().Date;

            if (localDate == localNow)
                return Resources.Today;

            if (localDate.AddDays(1) == localNow)
                return Resources.Yesterday;

            return date.ToLocalTime().ToString("ddd, dd MMM", cultureInfo ?? CultureInfo.CreateSpecificCulture("en-US"));
        }
    }
}
