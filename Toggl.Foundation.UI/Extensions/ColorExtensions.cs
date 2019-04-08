using System;
using MvvmCross.UI;
using Toggl.Foundation.Calendar;
using static System.Math;

namespace Toggl.Foundation.UI.Extensions
{
    public static class ColorExtensions
    {
        // Taken from http://referencesource.microsoft.com/#System.Drawing/commonui/System/Drawing/Color.cs,23adaaa39209cc1f
        public static (float hue, float saturation, float value) GetHSV(this MvxColor color)
        {
            var max = Max(color.R, Max(color.G, color.B));
            var min = Min(color.R, Min(color.G, color.B));

            return (
                hue: color.GetHue(),
                saturation: (float)((max <= 0) ? 0 : 1d - (1d * min / max)),
                value: (float)(max / 255d));
        }

        public static float GetHue(this MvxColor self)
        {
            if (self.R == self.G && self.G == self.B)
                return 0; // 0 makes as good an UNDEFINED value as any

            float r = (float)self.R / 255.0f;
            float g = (float)self.G / 255.0f;
            float b = (float)self.B / 255.0f;

            float max, min;
            float delta;
            float hue = 0.0f;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            delta = max - min;

            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f)
            {
                hue += 360.0f;
            }
            return hue / 360;
        }

        public static MvxColor ForegroundColor(this CalendarItem calendarItem)
        {
            // Adjusted relative luminance
            // math based on https://www.w3.org/WAI/GL/wiki/Relative_luminance

            var color = MvxColor.ParseHexString(calendarItem.Color);

            var rsrgb = color.R / 255.0;
            var gsrgb = color.G / 255.0;
            var bsrgb = color.B / 255.0;

            var lowGammaCoeficient = 1 / 12.92;

            var r = rsrgb <= 0.03928 ? rsrgb * lowGammaCoeficient : adjustGamma(rsrgb);
            var g = gsrgb <= 0.03928 ? gsrgb * lowGammaCoeficient : adjustGamma(gsrgb);
            var b = bsrgb <= 0.03928 ? bsrgb * lowGammaCoeficient : adjustGamma(bsrgb);

            var luma = r * 0.2126 + g * 0.7152 + b * 0.0722;

            return luma < 0.5 ? MvxColors.White : MvxColors.Black;

            double adjustGamma(double channel)
                => Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }
}
