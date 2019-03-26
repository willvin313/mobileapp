using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.ViewHolders
{
    public abstract class BaseRecyclerViewHolder<T> : RecyclerView.ViewHolder
    {
        private bool viewsAreInitialized = false;

        public ISubject<T> TappedSubject { get; set; }

        private T item;
        public T Item
        {
            get => item;
            set
            {
                item = value;

                if (!viewsAreInitialized)
                {
                    InitializeViews();
                    viewsAreInitialized = true;
                }

                UpdateView();
            }
        }

        protected BaseRecyclerViewHolder(View itemView)
            : base(itemView)
        {
            ItemView.Click += OnItemViewClick;
        }

        protected BaseRecyclerViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected abstract void InitializeViews();

        protected abstract void UpdateView();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || ItemView == null) return;
            ItemView.Click -= OnItemViewClick;
        }

        protected virtual void OnItemViewClick(object sender, EventArgs args)
        {
            TappedSubject?.OnNext(Item);
        }
    }
}
