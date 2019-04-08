using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Foundation;
using Intents;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Intents;
using Toggl.Daneel.Services;
using Toggl.Daneel.ViewControllers;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.UI;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.UI.ViewModels.Calendar;
using Toggl.Foundation.UI.ViewModels.Reports;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Multivac.Extensions;
using UIKit;
using UserNotifications;
using AdjustBindingsiOS;

namespace Toggl.Daneel
{
    [Register(nameof(AppDelegate))]
    public sealed class AppDelegate : MvxApplicationDelegate<Setup, App<OnboardingViewModel>>, IUNUserNotificationCenterDelegate
    {
        private IAnalyticsService analyticsService;
        private IBackgroundService backgroundService;
        private IMvxNavigationService navigationService;
        private ITimeService timeService;

        public override UIWindow Window { get; set; }

        private CompositeDisposable lastUpdateDateDisposable = new CompositeDisposable();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            #if USE_ANALYTICS
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_IOS}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
            Firebase.Core.App.Configure();
            Google.SignIn.SignIn.SharedInstance.ClientID =
                Firebase.Core.App.DefaultInstance.Options.ClientId;
            Adjust.AppDidLaunch(ADJConfig.ConfigWithAppToken("{TOGGL_ADJUST_APP_TOKEN}", AdjustConfig.EnvironmentProduction));
            #endif

            base.FinishedLaunching(application, launchOptions);

            UNUserNotificationCenter.Current.Delegate = this;

            #if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
            #endif

            return true;
        }

        protected override void RunAppStart(object hint = null)
        {
            base.RunAppStart(hint);

            var container = IosDependencyContainer.Instance;

            timeService = container.TimeService;
            analyticsService = container.AnalyticsService;
            backgroundService = container.BackgroundService;
            navigationService = container.NavigationService;
            setupNavigationBar();
            setupTabBar();
        }

        #if USE_ANALYTICS
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            var openUrlOptions = new UIApplicationOpenUrlOptions(options);

            return Google.SignIn.SignIn.SharedInstance.HandleUrl(url, openUrlOptions.SourceApplication, openUrlOptions.Annotation);
        }
        #endif

        public override void OnActivated(UIApplication application)
        {
            observeAndStoreLastUpdateDate();
        }

        public override void WillEnterForeground(UIApplication application)
        {
            base.WillEnterForeground(application);
            backgroundService.EnterForeground();
        }

        public override void DidEnterBackground(UIApplication application)
        {
            base.DidEnterBackground(application);
            backgroundService.EnterBackground();
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var eventId = response.Notification.Request.Content.UserInfo[NotificationServiceIos.CalendarEventIdKey] as NSString;

            if (response.IsCustomAction)
            {
                switch (response.ActionIdentifier.ToString())
                {
                    case NotificationServiceIos.OpenAndCreateFromCalendarEvent:
                        openAndStartTimeEntryFromCalendarEvent(eventId.ToString(), completionHandler);
                        break;
                    case NotificationServiceIos.OpenAndNavigateToCalendar:
                        openAndNavigateToCalendar(completionHandler);
                        break;
                    case NotificationServiceIos.StartTimeEntryInBackground:
                        startTimeEntryInBackground(eventId.ToString(), completionHandler);
                        break;
                }
            }
            else if (response.IsDefaultAction)
            {
                openAndStartTimeEntryFromCalendarEvent(eventId.ToString(), completionHandler);
            }
        }

        public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler)
        {
            analyticsService.ApplicationShortcut.Track(shortcutItem.LocalizedTitle);

            var key = new NSString(nameof(ApplicationShortcut.Type));
            if (!shortcutItem.UserInfo.ContainsKey(key))
                return;

            var shortcutNumber = shortcutItem.UserInfo[key] as NSNumber;
            if (shortcutNumber == null)
                return;

            var shortcutType = (ShortcutType)(int)shortcutNumber;

            switch (shortcutType)
            {
                case ShortcutType.ContinueLastTimeEntry:
                    navigationService.Navigate<MainViewModel>();
                    var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;
                    if (interactorFactory == null) return;
                    IDisposable subscription = null;
                    subscription = interactorFactory
                        .ContinueMostRecentTimeEntry()
                        .Execute()
                        .Subscribe(_ =>
                        {
                            subscription.Dispose();
                            subscription = null;
                        });
                    break;

                case ShortcutType.Reports:
                    navigationService.Navigate<ReportsViewModel>();
                    break;

                case ShortcutType.StartTimeEntry:
                    navigationService.Navigate<MainViewModel>();
                    navigationService.Navigate<StartTimeEntryViewModel>();
                    break;

                case ShortcutType.Calendar:
                    navigationService.Navigate<CalendarViewModel>();
                    break;
            }
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity,
            UIApplicationRestorationHandler completionHandler)
        {
            var interaction = userActivity.GetInteraction();
            if (interaction == null || interaction.IntentHandlingStatus != INIntentHandlingStatus.DeferredToApplication)
            {
                return false;
            }

            var intent = interaction?.Intent;

            switch (intent)
            {
                case StopTimerIntent _:
                    navigationService.Navigate(ApplicationUrls.Main.StopFromSiri);
                    return true;
                case ShowReportIntent _:
                    navigationService.Navigate(ApplicationUrls.Reports);
                    return true;
                case ShowReportPeriodIntent periodIntent:
                    var tabbarVC = (MainTabBarController)UIApplication.SharedApplication.KeyWindow.RootViewController;
                    var reportViewModel = (ReportsViewModel)tabbarVC.ViewModel.Tabs.Single(viewModel => viewModel is ReportsViewModel);
                    navigationService.Navigate(reportViewModel, periodIntent.Period.ToReportPeriod());
                    return true;
                case StartTimerIntent startTimerIntent:
                    var timeEntryParams = createStartTimeEntryParameters(startTimerIntent);
                    navigationService.Navigate<MainViewModel>();
                    navigationService.Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(timeEntryParams);
                    return true;
                default:
                    return false;
            }
        }

        private StartTimeEntryParameters createStartTimeEntryParameters(StartTimerIntent intent)
        {
            var tags = (intent.Tags == null || intent.Tags.Count() == 0)
                ? null
                : intent.Tags.Select(tagid => (long)Convert.ToDouble(tagid.Identifier));

            return new StartTimeEntryParameters(
                DateTimeOffset.Now,
                "",
                null,
                string.IsNullOrEmpty(intent.Workspace?.Identifier) ? null : (long?)Convert.ToDouble(intent.Workspace?.Identifier),
                intent.EntryDescription ?? "",
                string.IsNullOrEmpty(intent.ProjectId?.Identifier) ? null : (long?)Convert.ToDouble(intent.ProjectId?.Identifier),
                tags,
                TimeEntryStartOrigin.Siri
            );
        }

        public override void ApplicationSignificantTimeChange(UIApplication application)
        {
            timeService.SignificantTimeChanged();
        }

        private void setupTabBar()
        {
            UITabBar.Appearance.SelectedImageTintColor = Color.TabBar.SelectedImageTintColor.ToNativeColor();
            UITabBarItem.Appearance.TitlePositionAdjustment = new UIOffset(0, 200);
        }

        private void setupNavigationBar()
        {
            //Back button title
            var attributes = new UITextAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                TextColor = Color.NavigationBar.BackButton.ToNativeColor()
            };
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Normal);
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Highlighted);
            UIBarButtonItem.Appearance.SetBackButtonTitlePositionAdjustment(new UIOffset(6, 0), UIBarMetrics.Default);

            //Back button icon
            var image = UIImage.FromBundle("icBackNoPadding");
            UINavigationBar.Appearance.BackIndicatorImage = image;
            UINavigationBar.Appearance.BackIndicatorTransitionMaskImage = image;

            //Title and background
            var barBackgroundColor = Color.NavigationBar.BackgroundColor.ToNativeColor();
            UINavigationBar.Appearance.ShadowImage = new UIImage();
            UINavigationBar.Appearance.BarTintColor = barBackgroundColor;
            UINavigationBar.Appearance.BackgroundColor = barBackgroundColor;
            UINavigationBar.Appearance.TintColor = Color.NavigationBar.BackButton.ToNativeColor();
            UINavigationBar.Appearance.SetBackgroundImage(ImageExtension.ImageWithColor(barBackgroundColor), UIBarMetrics.Default);
            UINavigationBar.Appearance.TitleTextAttributes = new UIStringAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                ForegroundColor = UIColor.Black
            };
        }

        private void observeAndStoreLastUpdateDate()
        {
            lastUpdateDateDisposable.Dispose();
            lastUpdateDateDisposable = new CompositeDisposable();

            try
            {
                var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;
                var privateSharedStorage = IosDependencyContainer.Instance.PrivateSharedStorageService;

                interactorFactory.ObserveTimeEntriesChanges().Execute().StartWith(default(Unit))
                    .SelectMany(interactorFactory.GetAllTimeEntriesVisibleToTheUser().Execute())
                    .Select(timeEntries => timeEntries.OrderBy(te => te.At).Last().At)
                    .Subscribe(privateSharedStorage.SaveLastUpdateDate)
                    .DisposedBy(lastUpdateDateDisposable);
            }
            catch (Exception)
            {
                // Ignore errors when logged out
            }
        }

        #region Notification Actions

        private void openAndStartTimeEntryFromCalendarEvent(string eventId, Action completionHandler)
        {
            completionHandler();
            var url = ApplicationUrls.Calendar.ForId(eventId);
            navigationService.Navigate(url);
        }

        private void openAndNavigateToCalendar(Action completionHandler)
        {
            completionHandler();
            var url = ApplicationUrls.Calendar.Default;
            navigationService.Navigate(url);
        }

        private void startTimeEntryInBackground(string eventId, Action completionHandler)
        {
            var timeService = IosDependencyContainer.Instance.TimeService;
            var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;

            Task.Run(async () =>
            {
                var calendarItem = await interactorFactory.GetCalendarItemWithId(eventId).Execute();

                var now = timeService.CurrentDateTime;
                var workspace = await interactorFactory.GetDefaultWorkspace()
                    .TrackException<InvalidOperationException, IThreadSafeWorkspace>("AppDelegate.startTimeEntryInBackground")
                    .Execute();

                var prototype = calendarItem.Description.AsTimeEntryPrototype(now, workspace.Id);
                await interactorFactory.CreateTimeEntry(prototype, TimeEntryStartOrigin.CalendarNotification).Execute();
                completionHandler();
            });
        }

        #endregion

        #region Background Sync

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;
            interactorFactory?.RunBackgroundSync()
                .Execute()
                .Select(mapToNativeOutcomes)
                .Subscribe(completionHandler);
        }

        private UIBackgroundFetchResult mapToNativeOutcomes(Foundation.Models.SyncOutcome outcome) {
            switch (outcome)
            {
                case Foundation.Models.SyncOutcome.NewData:
                    return UIBackgroundFetchResult.NewData;
                case Foundation.Models.SyncOutcome.NoData:
                    return UIBackgroundFetchResult.NoData;
                case Foundation.Models.SyncOutcome.Failed:
                    return UIBackgroundFetchResult.Failed;
                default:
                    return UIBackgroundFetchResult.Failed;
            }
        }

        #endregion
    }
}
