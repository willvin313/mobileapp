using System;
using Android.Runtime;
using Android.Views;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac;

namespace Toggl.Giskard.Adapters
{
    public sealed class SimpleAdapter<T> : BaseRecyclerAdapter<T>
        where T : IDiffable<T>
    {
        private readonly int layoutId;
        private readonly Func<View, BaseRecyclerViewHolder<T>> createViewHolder;

        public SimpleAdapter(int layoutId, Func<View, BaseRecyclerViewHolder<T>> createViewHolder)
        {
            Ensure.Argument.IsNotNull(createViewHolder, nameof(createViewHolder));

            this.layoutId = layoutId;
            this.createViewHolder = createViewHolder;
        }

        public SimpleAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override BaseRecyclerViewHolder<T> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            var itemView = inflater.Inflate(layoutId, parent, false);
            return createViewHolder(itemView);
        }
    }
}
