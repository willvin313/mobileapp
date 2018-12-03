using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.Interactors
{
    [Preserve(AllMembers = true)]
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly IPlatformInfo platformInfo;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly INotificationService notificationService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ICalendarService calendarService;

        public InteractorFactory(
            IIdProvider idProvider,
            ITimeService timeService,
            IPlatformInfo platformInfo,
            ITogglDataSource dataSource,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            INotificationService notificationService,
            IIntentDonationService intentDonationService,
            IApplicationShortcutCreator shortcutCreator,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ICalendarService calendarService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(notificationService, nameof(notificationService));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));

            this.dataSource = dataSource;
            this.idProvider = idProvider;
            this.timeService = timeService;
            this.platformInfo = platformInfo;
            this.calendarService = calendarService;
            this.userPreferences = userPreferences;
            this.shortcutCreator = shortcutCreator;
            this.analyticsService = analyticsService;
            this.notificationService = notificationService;
            this.intentDonationService = intentDonationService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
        }
    }
}
