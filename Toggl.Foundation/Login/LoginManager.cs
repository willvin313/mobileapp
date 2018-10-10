using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Math = System.Math;

namespace Toggl.Foundation.Login
{
    public sealed class LoginManager : ILoginManager
    {
        private readonly IApiFactory apiFactory;
        private readonly ITogglDatabase database;
        private readonly IGoogleService googleService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IAnalyticsService analyticsService;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly Func<ITogglApi, ITogglDataSource> createDataSource;
        private readonly IScheduler scheduler;

        private readonly TimeSpan delayBeforeTryingToLogin = TimeSpan.FromSeconds(2);

        public LoginManager(
            IApiFactory apiFactory,
            ITogglDatabase database,
            IGoogleService googleService,
            IApplicationShortcutCreator shortcutCreator,
            IAccessRestrictionStorage accessRestrictionStorage,
            IAnalyticsService analyticsService,
            IPrivateSharedStorageService privateSharedStorageService,
            Func<ITogglApi, ITogglDataSource> createDataSource,
            IScheduler scheduler
        )
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(googleService, nameof(googleService));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(createDataSource, nameof(createDataSource));
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));

            this.database = database;
            this.apiFactory = apiFactory;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.googleService = googleService;
            this.analyticsService = analyticsService;
            this.privateSharedStorageService = privateSharedStorageService;
            this.shortcutCreator = shortcutCreator;
            this.createDataSource = createDataSource;
            this.scheduler = scheduler;
        }

        public IObservable<ITogglDataSource> Login(Email email, Password password)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to login");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to login");

            var credentials = Credentials.WithPassword(email, password);

            return database
                .Clear()
                .SelectMany(_ => apiFactory.CreateApiWith(credentials).User.Get())
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Track<ITogglDataSource, LoginSignupAuthenticationMethod, UserIsMissingApiTokenException>(
                    analyticsService.UserIsMissingApiToken,
                    LoginSignupAuthenticationMethod.Login)
                .RetryWhenUserIsMissingApiToken(scheduler);
        }

        public IObservable<ITogglDataSource> LoginWithGoogle()
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(loginWithGoogle);

        public IObservable<ITogglDataSource> SignUp(Email email, Password password, bool termsAccepted, int countryId)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to signup");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to signup");

            return database
                .Clear()
                .SelectMany(_ => signUp(email, password, termsAccepted, countryId))
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Track<ITogglDataSource, LoginSignupAuthenticationMethod, UserIsMissingApiTokenException>(
                    analyticsService.UserIsMissingApiToken,
                    LoginSignupAuthenticationMethod.SignUp)
                .Catch<ITogglDataSource, UserIsMissingApiTokenException>(_ => delayedLogin(email, password));
        }

        public IObservable<ITogglDataSource> SignUpWithGoogle()
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(signUpWithGoogle);

        public IObservable<string> ResetPassword(Email email)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to reset forgotten password.");

            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User.ResetPassword(email);
        }

        public ITogglDataSource GetDataSourceIfLoggedIn()
            => database.User
                .Single()
                .Select(dataSourceFromUser)
                .Catch(Observable.Return<ITogglDataSource>(null))
                .Do(shortcutCreator.OnLogin)
                .Wait();

        public IObservable<ITogglDataSource> RefreshToken(Password password)
        {
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to refresh token");

            return database.User
                .Single()
                .Select(user => user.Email)
                .Select(email => Credentials.WithPassword(email, password))
                .Select(apiFactory.CreateApiWith)
                .SelectMany(api => api.User.Get())
                .Select(User.Clean)
                .SelectMany(database.User.Update)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin);
        }

        private ITogglDataSource dataSourceFromUser(IUser user)
        {
            privateSharedStorageService.SaveApiToken(user.ApiToken);
            privateSharedStorageService.SaveUserId(user.Id);

            var newCredentials = Credentials.WithApiToken(user.ApiToken);
            var api = apiFactory.CreateApiWith(newCredentials);
            return createDataSource(api);
        }

        private IObservable<ITogglDataSource> loginWithGoogle(string googleToken)
        {
            var credentials = Credentials.WithGoogleToken(googleToken);

            return Observable
                .Return(apiFactory.CreateApiWith(credentials))
                .SelectMany(api => api.User.GetWithGoogle())
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Track<ITogglDataSource, LoginSignupAuthenticationMethod, UserIsMissingApiTokenException>(
                    analyticsService.UserIsMissingApiToken,
                    LoginSignupAuthenticationMethod.LoginGoogle)
                .RetryWhenUserIsMissingApiToken(scheduler);
        }

        private IObservable<IUser> signUp(Email email, Password password, bool termsAccepted, int countryId)
        {
            return apiFactory
                .CreateApiWith(Credentials.None)
                .User
                .SignUp(email, password, termsAccepted, countryId);
        }

        private IObservable<ITogglDataSource> delayedLogin(Email email, Password password)
            => Login(email, password).DelaySubscription(delayBeforeTryingToLogin, scheduler);

        private IObservable<ITogglDataSource> signUpWithGoogle(string googleToken)
        {
            return Observable
                .Return(googleToken)
                .SelectMany(apiFactory.CreateApiWith(Credentials.None).User.SignUpWithGoogle)
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(dataSourceFromUser)
                .Do(shortcutCreator.OnLogin)
                .Track<ITogglDataSource, LoginSignupAuthenticationMethod, UserIsMissingApiTokenException>(
                    analyticsService.UserIsMissingApiToken,
                    LoginSignupAuthenticationMethod.SignUpWithGoogle)
                .Catch<ITogglDataSource, UserIsMissingApiTokenException>(_ => delayedLoginWithGoogle(googleToken));
        }

        private IObservable<ITogglDataSource> delayedLoginWithGoogle(string googleToken)
            => loginWithGoogle(googleToken).DelaySubscription(delayBeforeTryingToLogin, scheduler);
    }
}
