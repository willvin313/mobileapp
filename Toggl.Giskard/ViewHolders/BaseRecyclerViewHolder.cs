using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.Themes;

namespace Toggl.Giskard.ViewHolders
{
    public abstract class BaseRecyclerViewHolder<T> : RecyclerView.ViewHolder
    {
        private IDisposable themeDisposable;
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

                    themeDisposable = AndroidDependencyContainer.Instance
                        .ThemeProvider.CurrentTheme
                        .Subscribe(UpdateTheme);
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

        protected virtual void UpdateTheme(ITheme theme)
        {

        }
        
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
