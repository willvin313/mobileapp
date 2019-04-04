using Android.Content;
using AndroidColor = Android.Graphics.Color;
using Toggl.Multivac;
using Android.Graphics.Drawables;
using Android.Util;

namespace Toggl.Giskard.Extensions
{
    public static class ColorExtensions
    {
        public static AndroidColor ToNativeColor(this Color color)
            => new AndroidColor(color.Red, color.Green, color.Blue, color.Alpha);

        public static GradientDrawable ToTransparentGradient(this Color color)
        {
            var argb = color.ToNativeColor().ToArgb();
            var transparent = color.WithAlpha(0).ToNativeColor();
            var colors = new[] { transparent.ToArgb(), argb, argb, argb };
            var gradientDrawable = new GradientDrawable(GradientDrawable.Orientation.LeftRight, colors);
            gradientDrawable.SetCornerRadius(0);
            return gradientDrawable;
        }

        public static Color GetThemedColor(this Context context, int attrId)
        {
            var theme = context.Theme;
            var typedValue = new TypedValue();
            theme.ResolveAttribute(attrId, typedValue, true);
            return new Color((uint)typedValue.Data);
        }
    }
}
