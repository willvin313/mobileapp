using System;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Fragments;
using Toggl.Multivac.Extensions;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class MainTabBarActivity : ReactiveActivity<MainTabBarViewModel>
    {
        private readonly Dictionary<int, Fragment> fragments = new Dictionary<int, Fragment>();
        private Fragment activeFragment;
        private bool activityResumedBefore = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var setup = MvxAndroidSetupSingleton.EnsureSingletonAvailable(ApplicationContext);
            setup.EnsureInitialized();

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainTabBarActivity);
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_fade_out);

            InitializeViews();
            showInitialFragment();

            navigationView
                .Rx()
                .ItemSelected()
                .Subscribe(onTabSelected)
                .DisposedBy(DisposeBag);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (!activityResumedBefore)
            {
                navigationView.SelectedItemId = Resource.Id.MainTabTimerItem;
                activityResumedBefore = true;
            }
        }

        private Fragment getCachedFragment(int itemId)
        {
            if (fragments.TryGetValue(itemId, out var fragment))
                return fragment;

            switch (itemId)
            {
                case Resource.Id.MainTabTimerItem:
                    var mainFragment = new MainFragment();
                    mainFragment.ViewModel = ViewModel.Tabs[0] as MainViewModel;
                    fragment = mainFragment;
                    break;
                case Resource.Id.MainTabReportsItem:
                    var reportsFragment = new ReportsFragment();
                    reportsFragment.ViewModel = ViewModel.Tabs[1] as ReportsViewModel;
                    fragment = reportsFragment;
                    break;
                case Resource.Id.MainTabCalendarItem:
                    var calendarFragment = new CalendarFragment();
                    calendarFragment.ViewModel = ViewModel.Tabs[2] as CalendarViewModel;
                    fragment = calendarFragment;
                    break;
                case Resource.Id.MainTabSettinsItem:
                    var settingsFragment = new SettingsFragment();
                    settingsFragment.ViewModel = ViewModel.Tabs[3] as SettingsViewModel;
                    fragment = settingsFragment;
                    break;
                default:
                    throw new ArgumentException($"Unexpected item id {itemId}");
            }
            fragments[itemId] = fragment;
            return fragment;
        }

        public override void OnBackPressed()
        {
            if (navigationView.SelectedItemId == Resource.Id.MainTabTimerItem)
            {
                base.OnBackPressed();
                return;
            }

            var fragment = getCachedFragment(Resource.Id.MainTabTimerItem);
            showFragment(fragment);

            navigationView.SelectedItemId = Resource.Id.MainTabTimerItem;
        }

        private void onTabSelected(IMenuItem item)
        {
            if (item.ItemId == navigationView.SelectedItemId)
                return;

            var fragment = getCachedFragment(item.ItemId);
            showFragment(fragment);
        }

        private void showFragment(Fragment fragment)
        {
            var transaction = SupportFragmentManager.BeginTransaction();

            if (activeFragment is MainFragment mainFragmentToHide)
                mainFragmentToHide.SetFragmentIsVisible(false);

            if (fragment.IsAdded)
                transaction.Hide(activeFragment).Show(fragment);
            else
                transaction.Add(Resource.Id.CurrentTabFragmmentContainer, fragment).Hide(activeFragment);

            transaction.Commit();

            if (fragment is MainFragment mainFragmentToShow)
                mainFragmentToShow.SetFragmentIsVisible(true);

            activeFragment = fragment;
        }

        private void showInitialFragment()
        {
            SupportFragmentManager.RemoveAllFragments();

            var mainFragment = getCachedFragment(Resource.Id.MainTabTimerItem) as MainFragment;
            SupportFragmentManager
                .BeginTransaction()
                .Add(Resource.Id.CurrentTabFragmmentContainer, mainFragment)
                .Commit();

            mainFragment.SetFragmentIsVisible(true);

            activeFragment = mainFragment;
        }

        internal void ToggleReportsCalendarState(bool forceHide)
        {
            var reportsFragment = getCachedFragment(Resource.Id.MainTabReportsItem) as ReportsFragment;
            reportsFragment.ToggleCalendarState(forceHide);
        }
    }
}
