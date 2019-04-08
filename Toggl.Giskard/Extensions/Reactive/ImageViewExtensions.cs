using System;
using Android.Content;
using Android.Support.V4.Content;
using Android.Widget;
using Toggl.Foundation.UI.Reactive;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class ImageViewExtensions
    {
        public static Action<int> Image(this IReactive<ImageView> reactive, Context context)
            => resource => reactive.Base.SetImageDrawable(ContextCompat.GetDrawable(context, resource));
    }
}
