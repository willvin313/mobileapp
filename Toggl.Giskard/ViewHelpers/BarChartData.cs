using System;
using System.Globalization;
using System.Linq;
using Toggl.Foundation.Conversions;
using Toggl.Foundation.UI.ViewModels.Reports;
using Toggl.Multivac;

namespace Toggl.Giskard.ViewHelpers
{
    public struct BarChartData
    {
        public readonly string StartDate;
        public readonly string EndDate;
        public readonly BarViewModel[] Bars;
        public readonly int MaximumHoursPerBar;
        public readonly BarChartDayLabel[] HorizontalLabels;
        public readonly bool WorkspaceIsBillable;

        private BarChartData(DateTimeOffset startDate, DateTimeOffset endDate, bool workspaceIsBillable, DateFormat dateFormat, BarViewModel[] bars, int maximumHoursPerBar, DateTimeOffset[] horizontalLegend)
        {
            StartDate = startDate.ToString(dateFormat.Short, CultureInfo.InvariantCulture);
            EndDate = endDate.ToString(dateFormat.Short, CultureInfo.InvariantCulture);
            Bars = bars;
            MaximumHoursPerBar = maximumHoursPerBar;
            WorkspaceIsBillable = workspaceIsBillable;
            if (horizontalLegend != null)
            {
                HorizontalLabels = horizontalLegend
                    .Select(date => new BarChartDayLabel(DateTimeOffsetConversion.ToDayOfWeekInitial(date), date.ToString(dateFormat.Short, CultureInfo.InvariantCulture)))
                    .ToArray();
            }
            else
            {
                HorizontalLabels = new BarChartDayLabel[0];
            }
        }

        public static BarChartData Create(DateTimeOffset startDate, DateTimeOffset endDate, bool workspaceIsBillable, DateFormat dateFormat, BarViewModel[] bars, int maximumHoursPerBar, DateTimeOffset[] horizontalLegend)
            => new BarChartData(startDate, endDate, workspaceIsBillable, dateFormat, bars, maximumHoursPerBar, horizontalLegend);
    }
}
