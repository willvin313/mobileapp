using System;
using Android.App;
using Android.Runtime;
using Android.Views;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Views
{
    public sealed class ReportsRecyclerViewHolder : MvxRecyclerViewHolder
    {
        private static readonly int lastContainerHeight;
        private static readonly int normalContainerHeight;

        static ReportsRecyclerViewHolder()
        {
            var context = Application.Context;

            lastContainerHeight = 72.DpToPixels(context);
            normalContainerHeight = 48.DpToPixels(context);
        }

        public bool IsLastItem { get; set; }

        public ReportsRecyclerViewHolder(View itemView, IMvxAndroidBindingContext context)
            : base(itemView, context)
        {
        }

        public ReportsRecyclerViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public void RecalculateSize()
        {
            var height = IsLastItem ? lastContainerHeight : normalContainerHeight;
            var layoutParameters = ItemView.LayoutParameters;
            layoutParameters.Height = height;
            ItemView.LayoutParameters = layoutParameters;
        }
    }

}
