using System.Collections.Immutable;
using System.Reactive.Linq;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Activities;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxFragmentPresentation(typeof(ReportsViewModel), Resource.Id.ReportsCalendarContainer, AddToBackStack = false)]
    public sealed partial class ReportsCalendarFragment : ReactiveFragment<ReportsCalendarViewModel>
    {
        private int rowHeight;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = InflateAndInitializeViews(Resource.Layout.ReportsCalendarFragment);

            rowHeight = Activity.Resources.DisplayMetrics.WidthPixels / 7;

            var currentMonthObservable = ViewModel.CurrentMonthName.CombineLatest(ViewModel.CurrentYear, formatMonthAndYear);

            var calendarAdapter = new CalendarPagerAdapter(Activity, ViewModel.CalendarDayTapped);
            pager.Adapter = calendarAdapter;
            pager.SetCurrentItem(11, false);

            var shortcutAdapter = new SimpleAdapter<QuickSelectShortcut>(
                Resource.Layout.ReportsCalendarShortcutCell,
                QuickSelectShortcutViewHolder.Create
            );

            shortcutAdapter.OnItemTapped = ViewModel.QuickSelect;
            shortcutsRecyclerView.SetAdapter(shortcutAdapter);
            shortcutsRecyclerView
                .SetLayoutManager(new LinearLayoutManager(Activity, LinearLayoutManager.Horizontal, false));

            // Text
            this.Bind(ViewModel.DayHeaders, bindHeaders);
            this.Bind(currentMonthObservable, calendarCurrentMonthTextView.BindText());

            // Shortcuts
            this.Bind(ViewModel.QuickSelectShortcuts, shortcutAdapter.BindItems());

            // Calendar
            this.Bind(ViewModel.Months, months => calendarAdapter.Months = months);
            this.Bind(ViewModel.CurrentPage, pager.BindCurrentPage());
            this.Bind(pager.CurrentPage(), ViewModel.OnCurrentPageChanged);
            this.Bind(ViewModel.RowsInCurrentMonth, recalculatePagerHeight);

            this.BindVoid(ViewModel.ReloadCalendar, () =>
            {   
                shortcutAdapter.NotifyDataSetChanged();
                calendarAdapter.RefreshPage(pager.CurrentItem);
            });

            return view;

            string formatMonthAndYear(string month, string year)
                => $"{month} {year}";

            void bindHeaders(IImmutableList<string> headers)
            {
                header.GetChildren<TextView>()
                    .Indexed()
                    .ForEach((textView, index)
                        => textView.Text = headers[index]);
            }

            void recalculatePagerHeight(int rowCount)
            {
                var layoutParams = pager.LayoutParameters;
                layoutParams.Height = rowHeight * rowCount;
                pager.LayoutParameters = layoutParams;

                var activity = (ReportsActivity)Activity;
                activity.RecalculateCalendarHeight();
            }
        }
    }
}
