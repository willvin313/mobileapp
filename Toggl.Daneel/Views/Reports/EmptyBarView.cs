using UIKit;
using Toggl.Foundation.UI.Helper;
using MvvmCross.Plugin.Color.Platforms.Ios;

namespace Toggl.Daneel.Views.Reports
{
    internal sealed class EmptyBarView : UIView
    {
        private const float bottomLineHeight = 1;

        public EmptyBarView()
        {
            var bottomLine = new UIView
            {
                BackgroundColor = Color.Reports.BarChart.EmptyBar.ToNativeColor(),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            AddSubview(bottomLine);

            bottomLine.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            bottomLine.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;
            bottomLine.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            bottomLine.HeightAnchor.ConstraintEqualTo(bottomLineHeight).Active = true;
        }
    }
}
