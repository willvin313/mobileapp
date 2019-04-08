using System;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.Transformations
{
    public static class DurationFormatToString
    {
        public static string Convert(DurationFormat value)
        {
            switch (value)
            {
                case DurationFormat.Classic:
                    return Resources.DurationFormatClassic;

                case DurationFormat.Improved:
                    return Resources.DurationFormatImproved;

                case DurationFormat.Decimal:
                    return Resources.DurationFormatDecimal;

                default:
                    throw new ArgumentException(
                        $"Duration format must be either: {nameof(DurationFormat.Classic)}, {nameof(DurationFormat.Improved)} or {nameof(DurationFormat.Decimal)}"
                    );
            }
        }
    }
}
