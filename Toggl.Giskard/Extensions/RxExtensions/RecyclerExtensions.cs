using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Toggl.Giskard.Adapters;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static Action<IImmutableList<T>> BindItems<T>(this BaseRecyclerAdapter<T> adapter)
            => collection => adapter.Items = collection;
    }
}
