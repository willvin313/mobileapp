using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Extensions
{
    public static class CanvasExtensions
    {
        public static Rect GetTextBounds(this Paint paint, string text)
        {
            var rect = new Rect();
            paint.GetTextBounds(text, 0, text.Length, rect);
            return rect;
        }
    }
}
