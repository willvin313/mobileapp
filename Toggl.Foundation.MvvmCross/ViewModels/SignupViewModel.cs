using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Location;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SignupViewModel : MvxViewModel<CredentialsParameter>
    {
        public enum ShakeTarget
        {
            None,
            Email,
            Password,
            Country
        }

        public enum State
        {
            Email,
            EmailAndPassword,
            CountrySelection
        }

        private readonly IApiFactory apiFactory;
        private readonly IUserAccessManager userAccessManager;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IForkingNavigationService navigationService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;
        private readonly ISchedulerProvider schedulerProvider;

        private List<ICountry> allCountries;

        private readonly Subject<ShakeTarget> shakeSubject = new Subject<ShakeTarget>();
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);
        private readonly int errorCountBeforeShowingContactSupportSuggestion = 2;
        private readonly Exception invalidEmailException = new Exception(Resources.EnterValidEmail);
        private readonly BehaviorSubject<State> state = new BehaviorSubject<State>(State.Email);
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public BehaviorRelay<string> EmailRelay { get; } = new BehaviorRelay<string>(string.Empty);
        public BehaviorRelay<string> PasswordRelay { get; } = new BehaviorRelay<string>(string.Empty);
        public BehaviorRelay<bool> TermsAccepted { get; } = new BehaviorRelay<bool>(false);
        public BehaviorRelay<ICountry> CountryRelay { get; } = new BehaviorRelay<ICountry>(null);
        public UIAction SignupWithGoogle { get; }
        public UIAction SignupWithEmail { get; }
        public UIAction SignUp { get; }
        public UIAction Back { get; }
        public UIAction TogglePasswordVisibility { get; }
        public UIAction GotoCountrySelection { get; }

        public IObservable<string> CountryButtonTitle { get; }
        public IObservable<bool> IsLoading { get; }
        public IObservable<ShakeTarget> Shake { get; }
        public IObservable<bool> IsPasswordMasked { get; }
        public IObservable<bool> IsShowPasswordButtonVisible { get; }
        public IObservable<Unit> ClearPasswordScreenError { get; }
        public IObservable<Unit> ClearEmailScreenError { get; }
        public IObservable<bool> IsEmailFieldEdittable { get; }
        public IObservable<bool> IsEmailScreenVisible { get; }
        public IObservable<bool> IsEmailAndPasswordScreenVisible { get; }
        public IObservable<bool> IsCountrySelectionScreenVisible { get; }
        public IObservable<bool> IsPasswordRuleMessageVisible { get; }

        public SignupViewModel(
            IApiFactory apiFactory,
            IUserAccessManager userAccessManager,
            IOnboardingStorage onboardingStorage,
            IForkingNavigationService navigationService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.apiFactory = apiFactory;
            this.userAccessManager = userAccessManager;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;
            this.schedulerProvider = schedulerProvider;

            var isEmailValid = EmailRelay
                .Select(email => Email.From(email).IsValid);

            var isPasswordValid = PasswordRelay
                .Select(password => Password.From(password).IsValid);

            Shake = shakeSubject.AsDriver(ShakeTarget.None, this.schedulerProvider);

            IsPasswordMasked = isPasswordMaskedSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            SignupWithGoogle = UIAction.FromObservable(signupWithGoogle);

            SignupWithEmail = UIAction.FromAction(signUpWithEmail);

            SignUp = UIAction.FromAction(() => throw new Exception());

            var isLoading = Observable
                .CombineLatest(SignUp.Executing, SignupWithGoogle.Executing, CommonFunctions.Or);

            IsLoading = isLoading.AsDriver(schedulerProvider);

            Back = UIAction.FromAction(back, isLoading.Invert());

            IsShowPasswordButtonVisible = PasswordRelay
                .Select(password => password.Length > 1)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            TogglePasswordVisibility = UIAction.FromAction(togglePasswordVisibility);

            ClearEmailScreenError = isEmailValid
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .ObserveOn(schedulerProvider.MainScheduler);

            IsEmailScreenVisible = state
                .Select(s => s == State.Email)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsEmailAndPasswordScreenVisible = state
                .Select(s => s == State.EmailAndPassword)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsCountrySelectionScreenVisible = state
                .Select(s => s == State.CountrySelection)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsPasswordMasked = isPasswordMaskedSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            ClearPasswordScreenError = Observable
                .CombineLatest(isEmailValid, isPasswordValid, CommonFunctions.And)
                .Merge(IsEmailScreenVisible)
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .ObserveOn(schedulerProvider.MainScheduler);

            GotoCountrySelection = UIAction.FromAction(gotoCountrySelection);
        }

        public override void Prepare(CredentialsParameter parameter)
        {
            EmailRelay.Accept(parameter.Email.ToString());
            PasswordRelay.Accept(parameter.Password.ToString());
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            allCountries = await new GetAllCountriesInteractor().Execute();
            var api = apiFactory.CreateApiWith(Credentials.None);
            var interactor = new GetCurrentLocationInteractor(api);

            interactor
                .Execute()
                .Select(location => allCountries.Single(country => country.CountryCode == location.CountryCode))
                .Take(1)
                .Subscribe(CountryRelay.Accept)
                .DisposedBy(disposeBag);
        }

        public override void ViewDisappeared()
        {
            base.ViewDisappeared();
            disposeBag.Dispose();
        }

        private void togglePasswordVisibility()
            => isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value);

        private void gotoCountrySelection()
        {
            var password = Password.From(PasswordRelay.Value);
            var email = Email.From(EmailRelay.Value);

            if (!email.IsValid)
            {
                shakeSubject.OnNext(ShakeTarget.Email);
                throw invalidEmailException;
            }

            if (!password.IsValid)
            {
                shakeSubject.OnNext(ShakeTarget.Password);
                throw new Exception(Resources.PasswordTooShort);
            }

            state.OnNext(State.CountrySelection);
        }

        private IObservable<Unit> signupWithGoogle()
        {
            throw new Exception("not implemented");
        }

        private void signUpWithEmail()
        {
            if (!Email.From(EmailRelay.Value).IsValid)
            {
                shakeSubject.OnNext(ShakeTarget.Email);
                throw invalidEmailException;
            }

            state.OnNext(State.EmailAndPassword);
        }

        private void back()
        {
            switch (state.Value)
            {
                case State.Email:
                    navigationService.Close(this);
                    break;
                case State.EmailAndPassword:
                    state.OnNext(State.Email);
                    PasswordRelay.Accept(string.Empty);
                    break;
                case State.CountrySelection:
                    state.OnNext(State.EmailAndPassword);
                    break;
            }
        }
    }
}
