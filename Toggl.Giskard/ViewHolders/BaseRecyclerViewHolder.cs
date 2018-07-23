using System;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.ViewHolders
{
    public abstract class BaseRecyclerViewHolder<T> : RecyclerView.ViewHolder
    {
        public Action<T> Tapped { get; set; }

        private T item;
        public T Item 
        { 
            get => item;
            set
            {
                item = value;
                UpdateView();
            }
        }

        protected BaseRecyclerViewHolder(View itemView)
            : base(itemView)
        {
            ItemView.Click += onItemViewClick;
            InitializeViews();
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
            ItemView.Click -= onItemViewClick;
        }

        private void onItemViewClick(object sender, EventArgs args)
        {
            Tapped?.Invoke(Item);
        }
    }
}