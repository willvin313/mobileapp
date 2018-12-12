using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LoginViewModel : MvxViewModel<CredentialsParameter>
    {
        [Flags]
        public enum ShakeTargets
        {
            None = 0,
            Email = 1,
            Password = 2
        }

        private readonly IUserAccessManager userAccessManager;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IForkingNavigationService navigationService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly Subject<ShakeTargets> shakeSubject = new Subject<ShakeTargets>();
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);
        private readonly int errorCountBeforeShowingContactSupportSuggestion = 2;

        public bool IsPasswordManagerAvailable { get; }

        public BehaviorRelay<string> EmailRelay { get; } = new BehaviorRelay<string>(string.Empty);

        public BehaviorRelay<string> PasswordRelay { get; } = new BehaviorRelay<string>(string.Empty);

        public IObservable<bool> IsLoggingIn { get; }

        public IObservable<ShakeTargets> Shake { get; }

        public IObservable<bool> IsPasswordMasked { get; }

        public IObservable<bool> IsShowPasswordButtonVisible { get; }

        public IObservable<bool> SuggestContactSupport { get; }

        public UIAction LoginWithEmail { get; }

        public UIAction LoginWithGoogle { get; }

        public UIAction TogglePasswordVisibility { get; }

        public UIAction ForgotPassword { get; }

        public UIAction ContinueToPaswordScreen { get; }

        public LoginViewModel(
            IUserAccessManager userAccessManager,
            IOnboardingStorage onboardingStorage,
            IForkingNavigationService navigationService,
            IPasswordManagerService passwordManagerService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(passwordManagerService, nameof(passwordManagerService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.userAccessManager = userAccessManager;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.schedulerProvider = schedulerProvider;

            Shake = shakeSubject
                .AsDriver(ShakeTargets.None, this.schedulerProvider);

            IsPasswordMasked = isPasswordMaskedSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsShowPasswordButtonVisible = PasswordRelay
                .Select(password => password.Length > 1)
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsPasswordManagerAvailable = passwordManagerService.IsAvailable;

            LoginWithEmail = UIAction.FromObservable(login);

            LoginWithGoogle = UIAction.FromObservable(loginWithGoogle);

            IsLoggingIn = Observable
                .CombineLatest(LoginWithEmail.Executing, LoginWithGoogle.Executing, CommonFunctions.Or)
                .AsDriver(schedulerProvider);

            ForgotPassword = UIAction.FromObservable(() => forgotPassword(EmailRelay.Value));

            TogglePasswordVisibility =
                UIAction.FromAction(() => isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value));

            ContinueToPaswordScreen = UIAction.FromObservable(continueToPasswordScreen);

            SuggestContactSupport = Observable.Merge(LoginWithEmail.Errors, LoginWithGoogle.Errors)
                .Skip(errorCountBeforeShowingContactSupportSuggestion)
                .SelectValue(true)
                .StartWith(false)
                .AsDriver(false, schedulerProvider);
        }

        public override void Prepare(CredentialsParameter parameter)
        {
            EmailRelay.Accept(parameter.Email.ToString());
            PasswordRelay.Accept(parameter.Password.ToString());
        }

        private IObservable<Unit> continueToPasswordScreen()
        {
            if (!Email.From(EmailRelay.Value).IsValid)
            {
                return Observable.Throw<Unit>(new Exception(Resources.EnterValidEmail));
            }

            return Observable.Return(Unit.Default);
        }

        private IObservable<Unit> loginWithGoogle()
            => userAccessManager
                .LoginWithGoogle()
                .SelectMany(onLoginSuccessfully)
                .Catch<Unit, Exception>(e => handleException(e))
                .ObserveOn(schedulerProvider.MainScheduler);

        private IObservable<Unit> login()
        {
            var password = Password.From(PasswordRelay.Value);
            if (!password.IsValid)
            {
                return Observable.Throw<Unit>(new Exception(Resources.PasswordTooShort));
            }

            return userAccessManager
                .Login(Email.From(EmailRelay.Value), Password.From(PasswordRelay.Value))
                .SelectMany(onLoginSuccessfully)
                .Catch<Unit, Exception>(e => handleException(e))
                .ObserveOn(schedulerProvider.MainScheduler);
        }

        private IObservable<Unit> handleException(Exception e)
        {
            if (errorHandlingService.TryHandleDeprecationError(e))
            {
                return Observable.Return(Unit.Default);
            }

            switch (e)
            {
                case UnauthorizedException _:
                    return Observable.Throw<Unit>(new Exception(Resources.IncorrectEmailOrPassword));
                case GoogleLoginException googleEx:
                    return Observable.Throw<Unit>(new Exception(googleEx.Message));
                default:
                    return Observable.Throw<Unit>(new Exception(Resources.GenericLoginError));
            }
        }

        private IObservable<Unit> onLoginSuccessfully(ITogglDataSource dataSource)
            => Observable.Defer(async () =>
                {
                    lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime);
                    await dataSource.StartSyncing();
                    onboardingStorage.SetIsNewUser(false);
                    return navigationService.ForkNavigate<MainTabBarViewModel, MainViewModel>().ToObservable();
                });

        private IObservable<Unit> forgotPassword(string email)
        {
            var emailParam = EmailParameter.With(Email.From(email));
            return navigationService
                .Navigate<ForgotPasswordViewModel, EmailParameter, EmailParameter>(emailParam)
                .ToObservable()
                .Do(result => PasswordRelay.Accept(result.ToString()))
                .SelectUnit()
                .ObserveOn(schedulerProvider.MainScheduler);
        }
    }
}
