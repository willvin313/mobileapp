using UIKit;
using Toggl.Foundation.UI.Helper;
using MvvmCross.Plugin.Color.Platforms.Ios;

namespace Toggl.Daneel.Views.Reports
{
    internal sealed class BarLegendLabel : UILabel
    {
        public BarLegendLabel(string dayInitial, string shortDate)
        {
            Text = $"{dayInitial}\n{shortDate}";
            TextAlignment = UITextAlignment.Center;
            Lines = 2;
            TextColor = Color.Reports.BarChart.Legend.ToNativeColor();
            Font = UIFont.SystemFontOfSize(12);
            AdjustsFontSizeToFitWidth = true;
        }
    }
}
