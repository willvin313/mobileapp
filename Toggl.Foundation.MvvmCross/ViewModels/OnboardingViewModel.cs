using System.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using MvvmCross.UI;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.UserAccess;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class OnboardingViewModel : MvxViewModel
    {
        public const int TrackPage = 0;
        public const int MostUsedPage = 1;
        public const int ReportsPage = 2;

        private static readonly string[] pageNames =
        {
            nameof(TrackPage),
            nameof(MostUsedPage),
            nameof(ReportsPage)
        };

        private static readonly (MvxColor BackgroundColor, MvxColor BorderColor)[] pageInfo =
        {
            (Color.Onboarding.TrackPageBackgroundColor, Color.Onboarding.TrackPageBorderColor),
            (Color.Onboarding.LogPageBackgroundColor, Color.Onboarding.LogPageBorderColor),
            (Color.Onboarding.ReportsPageBackgroundColor, Color.Onboarding.ReportsPageBorderColor)
        };

        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IMvxNavigationService navigationService;

        private readonly bool[] pagesVisited = new bool[pageInfo.Length];

        private bool visitedAllPages => pagesVisited.All(visited => visited);

        private int currentPage;

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage == value || value < 0)
                    return;

                if (value >= NumberOfPages)
                {
                    completeOnboarding();
                    return;
                }

                currentPage = value;
                pagesVisited[value] = true;
                RaisePropertyChanged();
                NextCommand.RaiseCanExecuteChanged();
                PreviousCommand.RaiseCanExecuteChanged();
            }
        }

        [DependsOn(nameof(CurrentPage))]
        public bool IsTrackPage => CurrentPage == TrackPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsMostUsedPage => CurrentPage == MostUsedPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsSummaryPage => CurrentPage == ReportsPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsFirstPage => CurrentPage == 0;

        [DependsOn(nameof(CurrentPage))]
        public bool IsLastPage => CurrentPage == NumberOfPages - 1;

        [DependsOn(nameof(CurrentPage))]
        public MvxColor BorderColor => pageInfo[CurrentPage].BorderColor;

        [DependsOn(nameof(CurrentPage))]
        public MvxColor BackgroundColor => pageInfo[CurrentPage].BackgroundColor;

        public IMvxAsyncCommand SkipCommand { get; }

        public IMvxAsyncCommand NextCommand { get; }

        public IMvxCommand PreviousCommand { get; }

        public int NumberOfPages => pageInfo.Length;

        public OnboardingViewModel(
            IMvxNavigationService navigationService,
            IOnboardingStorage onboardingStorage,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));

            this.analyticsService = analyticsService;
            this.navigationService = navigationService;
            this.onboardingStorage = onboardingStorage;

            SkipCommand = new MvxAsyncCommand(skip);
            NextCommand = new MvxAsyncCommand(next);
            PreviousCommand = new MvxCommand(previous, () => !IsFirstPage);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            if (onboardingStorage.CompletedOnboarding())
            {
                await navigationService.Navigate<SignupOrLoginChoiceViewModel>();
            }
            else
            {
                currentPage = TrackPage;
                pagesVisited[currentPage] = true;
            }
        }

        private async Task skip()
        {
            analyticsService.OnboardingSkip.Track(pageNames[CurrentPage]);
            await navigationService.Navigate<SignupOrLoginChoiceViewModel>();
        }

        private async Task next()
        {
            if (IsLastPage)
            {
                await completeOnboarding();
            }
            else
            {
                CurrentPage++;
            }
        }

        private Task completeOnboarding()
        {
            if (visitedAllPages)
            {
                onboardingStorage.SetCompletedOnboarding();
            }

            return navigationService.Navigate<SignupOrLoginChoiceViewModel>();
        }

        private void previous()
        {
            CurrentPage--;
        }
    }
}
