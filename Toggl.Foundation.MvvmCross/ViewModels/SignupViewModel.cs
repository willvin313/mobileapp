using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Location;
using Toggl.Foundation.Login;
using Toggl.Foundation.Models;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
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
        private readonly Exception incorrectPasswordException = new Exception(Resources.IncorrectEmailOrPassword);
        private readonly Exception emailIsAlreadyUsedException = new Exception(Resources.EmailIsAlreadyUsedError);
        private readonly Exception missingCountryException = new Exception(Resources.SignUpCountryRequired);
        private readonly Exception tosNotAcceptedException = new Exception(Resources.TOSAgreeRequired);
        private readonly BehaviorSubject<bool> tosAccepted = new BehaviorSubject<bool>(false);
        private readonly BehaviorSubject<ICountry> selectedCountry = new BehaviorSubject<ICountry>(null);

        public BehaviorRelay<string> EmailRelay { get; } = new BehaviorRelay<string>(string.Empty);
        public BehaviorRelay<string> PasswordRelay { get; } = new BehaviorRelay<string>(string.Empty);
        public UIAction SignupWithGoogle { get; }
        public UIAction SignupWithEmail { get; }
        public UIAction SignUp { get; }
        public UIAction Back { get; }
        public UIAction TogglePasswordVisibility { get; }
        public UIAction GotoCountrySelection { get; }
        public UIAction ToggleTOSAgreement { get; }
        public UIAction OpenCountryPicker { get; }

        public IObservable<bool> IsLoading { get; }
        public IObservable<ShakeTarget> Shake { get; }
        public IObservable<bool> IsPasswordMasked { get; }
        public IObservable<bool> IsShowPasswordButtonVisible { get; }
        public IObservable<Unit> ClearPasswordScreenError { get; }
        public IObservable<Unit> ClearEmailScreenError { get; }
        public IObservable<bool> IsEmailScreenVisible { get; }
        public IObservable<bool> IsEmailAndPasswordScreenVisible { get; }
        public IObservable<bool> IsCountrySelectionScreenVisible { get; }
        public IObservable<bool> IsPasswordRuleMessageVisible { get; }
        public IObservable<string> CountryNameLabel { get; }
        public IObservable<bool> CountryErrorLabelVisible { get; }
        public IObservable<bool> TOSErrorLabelVisible { get; }
        public IObservable<bool> TOSAccepted { get; }

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

            SignUp = UIAction.FromObservable(signup);

            var isLoading = Observable
                .CombineLatest(SignUp.Executing, SignupWithGoogle.Executing, CommonFunctions.Or);

            IsLoading = isLoading.AsDriver(schedulerProvider);

            Back = UIAction.FromAction(back, isLoading.Invert());

            ToggleTOSAgreement = UIAction.FromAction(toggleTOSAgreement);

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
                .Merge(IsEmailScreenVisible.Skip(1))
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .ObserveOn(schedulerProvider.MainScheduler);

            GotoCountrySelection = UIAction.FromAction(gotoCountrySelection);

            CountryNameLabel = selectedCountry
                .Select(country => country?.Name ?? "")
                .AsDriver(string.Empty, schedulerProvider);

            OpenCountryPicker = UIAction.FromObservable(openCountryPicker);

            CountryErrorLabelVisible = selectedCountry
                .Select(country => country == null)
                .AsDriver(schedulerProvider);

            TOSErrorLabelVisible = SignUp.Errors
                .Select(exception => exception == tosNotAcceptedException)
                .AsDriver(schedulerProvider);

            TOSAccepted = tosAccepted
                .AsDriver(schedulerProvider);
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
                .Subscribe(a => selectedCountry.OnNext(a))
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

        private void toggleTOSAgreement()
        {
            tosAccepted.OnNext(!tosAccepted.Value);
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

        private IObservable<Unit> openCountryPicker() => navigationService
            .Navigate<SelectCountryViewModel, long?, long?>(selectedCountry?.Value?.Id)
            .ToObservable()
            .Do(selectedCountryId =>
                selectedCountry.OnNext(allCountries.Single(country => country.Id == selectedCountryId.Value)))
            .SelectUnit()
            .ObserveOn(schedulerProvider.MainScheduler);

        private IObservable<Unit> signup()
        {
            if (selectedCountry.Value == null)
            {
                return Observable.Return(Unit.Default).Do(_ =>
                {
                    shakeSubject.OnNext(ShakeTarget.Country);
                    throw missingCountryException;
                });
            }

            if (!tosAccepted.Value)
            {
                return Observable.Throw<Unit>(tosNotAcceptedException);
            }

            return userAccessManager
                .SignUp(Email.From(EmailRelay.Value), Password.From(PasswordRelay.Value), true, (int)selectedCountry.Value.Id)
                .SelectMany(onSignupSuccessfully)
                .Catch<Unit, Exception>(handleException)
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
                    return Observable.Throw<Unit>(incorrectPasswordException);
                case EmailIsAlreadyUsedException _:
                    return Observable.Throw<Unit>(emailIsAlreadyUsedException);
                default:
                    return Observable.Throw<Unit>(new Exception(Resources.GenericSignUpError));
            }
        }

        private IObservable<Unit> onSignupSuccessfully(ITogglDataSource dataSource) => Observable.Return(Unit.Default)
            .Do(_ => lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime))
            .SelectMany(_ => dataSource.StartSyncing())
            .Do(_ => onboardingStorage.SetIsNewUser(true))
            .Do(_ => onboardingStorage.SetUserSignedUp())
            .SelectMany(_ => navigationService.ForkNavigate<MainTabBarViewModel, MainViewModel>().ToObservable());
    }
}
