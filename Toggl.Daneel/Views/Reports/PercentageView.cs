using System;
using UIKit;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using Toggl.Foundation.UI.Helper;
using MvvmCross.Plugin.Color.Platforms.Ios;

namespace Toggl.Daneel.Views.Reports
{
    [Register(nameof(PercentageView))]
    public sealed class PercentageView : UIView
    {
        private static readonly UIColor disabledColor = Color.Reports.PercentageDisabled.ToNativeColor();
        private static readonly UIColor activeColor = Color.Reports.PercentageActivatedBackground.ToNativeColor();

        private readonly CALayer percentageFilledLayer = new CALayer();

        private float? percentage = null;
        public float? Percentage
        {
            get => percentage;
            set
            {
                if (percentage == value) return;
                percentage = value;

                BackgroundColor = percentage.HasValue ? activeColor : disabledColor;

                var width = value == 0 ? 0 : (Frame.Width / 100) * (percentage ?? 0);
                percentageFilledLayer.Frame = new CGRect(0, 0, width, Frame.Height);
            }
        }

        public PercentageView(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            Layer.AddSublayer(percentageFilledLayer);
            percentageFilledLayer.BackgroundColor = Color.Reports.PercentageActivated.ToNativeColor().CGColor;
            percentageFilledLayer.Frame = new CGRect(0, 0, 0, Frame.Height);
            percentageFilledLayer.CornerRadius = Layer.CornerRadius;
        }
    }
}