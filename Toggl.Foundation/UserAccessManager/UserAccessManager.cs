using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Login
{
    public sealed class UserAccessManager : IUserAccessManager
    {
        private readonly Lazy<IApiFactory> apiFactory;
        private readonly Lazy<ITogglDatabase> database;
        private readonly Lazy<IGoogleService> googleService;
        private readonly Lazy<IPrivateSharedStorageService> privateSharedStorageService;

        private readonly ISubject<ITogglApi> userLoggedInSubject = new Subject<ITogglApi>();
        private readonly ISubject<Unit> userLoggedOutSubject = new Subject<Unit>();

        public IObservable<ITogglApi> UserLoggedIn => userLoggedInSubject.AsObservable();
        public IObservable<Unit> UserLoggedOut => userLoggedOutSubject.AsObservable();

        public UserAccessManager(
            Lazy<IApiFactory> apiFactory,
            Lazy<ITogglDatabase> database,
            Lazy<IGoogleService> googleService,
            Lazy<IPrivateSharedStorageService> privateSharedStorageService)
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(googleService, nameof(googleService));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));

            this.database = database;
            this.apiFactory = apiFactory;
            this.googleService = googleService;
            this.privateSharedStorageService = privateSharedStorageService;
        }

        public IObservable<Unit> Login(Email email, Password password)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to login");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to login");

            var credentials = Credentials.WithPassword(email, password);

            return database.Value
                .Clear()
                .SelectMany(_ => apiFactory.Value.CreateApiWith(credentials).User.Get())
                .Select(User.Clean)
                .SelectMany(database.Value.User.Create)
                .Select(apiFromUser)
                .Do(userLoggedInSubject.OnNext)
                .SelectUnit();
        }

        public IObservable<Unit> LoginWithGoogle()
            => database.Value
                .Clear()
                .SelectMany(_ => googleService.Value.LogOutIfNeeded())
                .SelectMany(_ => googleService.Value.GetAuthToken())
                .SelectMany(loginWithGoogle);

        public IObservable<Unit> SignUp(Email email, Password password, bool termsAccepted, int countryId, string timezone)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to signup");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to signup");

            return database.Value
                .Clear()
                .SelectMany(_ => signUp(email, password, termsAccepted, countryId, timezone))
                .Select(User.Clean)
                .SelectMany(database.Value.User.Create)
                .Select(apiFromUser)
                .Do(userLoggedInSubject.OnNext)
                .SelectUnit();
        }

        public IObservable<Unit> SignUpWithGoogle(bool termsAccepted, int countryId, string timezone)
            => database.Value
                .Clear()
                .SelectMany(_ => googleService.Value.LogOutIfNeeded())
                .SelectMany(_ => googleService.Value.GetAuthToken())
                .SelectMany(authToken => signUpWithGoogle(authToken, termsAccepted, countryId, timezone));

        public IObservable<string> ResetPassword(Email email)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to reset forgotten password.");

            var api = apiFactory.Value.CreateApiWith(Credentials.None);
            return api.User.ResetPassword(email);
        }

        public bool CheckIfLoggedIn()
            => database.Value
                .User.Single()
                .Do(user => userLoggedInSubject.OnNext(apiFromUser(user)))
                .SelectValue(true)
                .Catch(Observable.Return(false))
                .Wait();

        public IObservable<Unit> RefreshToken(Password password)
        {
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to refresh token");

            return database.Value.User
                .Single()
                .Select(user => user.Email)
                .Select(email => Credentials.WithPassword(email, password))
                .Select(apiFactory.Value.CreateApiWith)
                .SelectMany(api => api.User.Get())
                .Select(User.Clean)
                .SelectMany(database.Value.User.Update)
                .Select(apiFromUser)
                .Do(userLoggedInSubject.OnNext)
                .SelectUnit();
        }

        public void OnUserLoggedOut()
        {
            userLoggedOutSubject.OnNext(Unit.Default);
        }

        private ITogglApi apiFromUser(IUser user)
        {
            privateSharedStorageService.Value.SaveApiToken(user.ApiToken);
            privateSharedStorageService.Value.SaveUserId(user.Id);

            var newCredentials = Credentials.WithApiToken(user.ApiToken);
            var api = apiFactory.Value.CreateApiWith(newCredentials);
            return api;
        }

        private IObservable<Unit> loginWithGoogle(string googleToken)
        {
            var credentials = Credentials.WithGoogleToken(googleToken);

            return Observable
                .Return(apiFactory.Value.CreateApiWith(credentials))
                .SelectMany(api => api.User.GetWithGoogle())
                .Select(User.Clean)
                .SelectMany(database.Value.User.Create)
                .Select(apiFromUser)
                .Do(userLoggedInSubject.OnNext)
                .SelectUnit();
        }

        private IObservable<IUser> signUp(Email email, Password password, bool termsAccepted, int countryId, string timezone)
        {
            return apiFactory.Value
                .CreateApiWith(Credentials.None)
                .User
                .SignUp(email, password, termsAccepted, countryId, timezone);
        }


        private IObservable<Unit> signUpWithGoogle(string googleToken, bool termsAccepted, int countryId, string timezone)
        {
            var api = apiFactory.Value.CreateApiWith(Credentials.None);
            return api.User
                .SignUpWithGoogle(googleToken, termsAccepted, countryId, timezone)
                .Select(User.Clean)
                .SelectMany(database.Value.User.Create)
                .Select(apiFromUser)
                .Do(userLoggedInSubject.OnNext)
                .SelectUnit();
        }
    }
}
