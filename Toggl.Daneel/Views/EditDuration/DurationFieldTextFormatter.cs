using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using UIKit;
using Color = Toggl.Foundation.UI.Helper.Color;
using Toggl.Daneel.Extensions;
using Toggl.Multivac;

namespace Toggl.Daneel.Views.EditDuration
{
    public static class DurationFieldTextFormatter
    {
        private static readonly UIColor placeHolderColor = Color.Common.PlaceholderText.ToNativeColor();

        public static NSAttributedString AttributedStringFor(string durationText, UIFont font)
        {
            var prefixLength = DurationHelper.LengthOfDurationPrefix(durationText);
            var result = new NSMutableAttributedString(durationText, font: font.GetMonospacedDigitFont(), foregroundColor: UIColor.Black);
            result.AddAttribute(UIStringAttributeKey.ForegroundColor, placeHolderColor, new NSRange(0, prefixLength));
            return result;
        }
    }
}
