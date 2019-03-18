using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Login
{
    public sealed class UserAccessManager : IUserAccessManager
    {
        private readonly IApiFactory apiFactory;
        private readonly ITogglDatabase database;
        private readonly IGoogleService googleService;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly Func<ITogglApi, (ISyncManager, IInteractorFactory)> initializeAfterLogin;

        private readonly ISubject<Unit> userLoggedInSubject = new Subject<Unit>();
        private readonly ISubject<Unit> userLoggedOutSubject = new Subject<Unit>();

        public IObservable<Unit> UserLoggedIn => userLoggedInSubject.AsObservable();
        public IObservable<Unit> UserLoggedOut => userLoggedOutSubject.AsObservable();

        public UserAccessManager(
            IApiFactory apiFactory,
            ITogglDatabase database,
            IGoogleService googleService,
            IPrivateSharedStorageService privateSharedStorageService,
            Func<ITogglApi, (ISyncManager, IInteractorFactory)> initializeAfterLogin
        )
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(googleService, nameof(googleService));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(initializeAfterLogin, nameof(initializeAfterLogin));

            this.database = database;
            this.apiFactory = apiFactory;
            this.googleService = googleService;
            this.initializeAfterLogin = initializeAfterLogin;
            this.privateSharedStorageService = privateSharedStorageService;
        }

        public IObservable<ISyncManager> Login(Email email, Password password)
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
                .Select(initializeFor)
                .Select(initializedServices => initializedServices.syncManager)
                .Do(_ => userLoggedInSubject.OnNext(Unit.Default));
        }

        public IObservable<ISyncManager> LoginWithGoogle()
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(loginWithGoogle);

        public IObservable<ISyncManager> SignUp(Email email, Password password, bool termsAccepted, int countryId, string timezone)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to signup");
            if (!password.IsValid)
                throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to signup");

            return database
                .Clear()
                .SelectMany(_ => signUp(email, password, termsAccepted, countryId, timezone))
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(initializeFor)
                .Select(initializedServices => initializedServices.syncManager);
        }

        public IObservable<ISyncManager> SignUpWithGoogle(bool termsAccepted, int countryId, string timezone)
            => database
                .Clear()
                .SelectMany(_ => googleService.LogOutIfNeeded())
                .SelectMany(_ => googleService.GetAuthToken())
                .SelectMany(authToken => signUpWithGoogle(authToken, termsAccepted, countryId, timezone));

        public IObservable<string> ResetPassword(Email email)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to reset forgotten password.");

            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User.ResetPassword(email);
        }

        public bool TryInitializingAccessToUserData(out ISyncManager syncManager, out IInteractorFactory interactorFactory)
        {
            try
            {
                var user = database.User.Single().Wait();

                (syncManager, interactorFactory) = initializeFor(user);

                userLoggedInSubject.OnNext(Unit.Default);

                return true;
            }
            catch
            {
                syncManager = null;
                interactorFactory = null;
                return false;
            }
        }


        public IObservable<ISyncManager> RefreshToken(Password password)
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
                .Select(initializeFor)
                .Select(initializedServices => initializedServices.syncManager)
                .Do(_ => userLoggedInSubject.OnNext(Unit.Default));
        }

        public void OnUserLoggedOut()
        {
            userLoggedOutSubject.OnNext(Unit.Default);
        }

        private (ISyncManager syncManager, IInteractorFactory interactorFactory) initializeFor(IUser user)
        {
            privateSharedStorageService.SaveApiToken(user.ApiToken);
            privateSharedStorageService.SaveUserId(user.Id);

            var newCredentials = Credentials.WithApiToken(user.ApiToken);
            var api = apiFactory.CreateApiWith(newCredentials);
            return initializeAfterLogin(api);
        }

        private IObservable<ISyncManager> loginWithGoogle(string googleToken)
        {
            var credentials = Credentials.WithGoogleToken(googleToken);

            return Observable
                .Return(apiFactory.CreateApiWith(credentials))
                .SelectMany(api => api.User.GetWithGoogle())
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(initializeFor)
                .Select(initializedServices => initializedServices.syncManager)
                .Do(_ => userLoggedInSubject.OnNext(Unit.Default));
        }

        private IObservable<IUser> signUp(Email email, Password password, bool termsAccepted, int countryId, string timezone)
        {
            return apiFactory
                .CreateApiWith(Credentials.None)
                .User
                .SignUp(email, password, termsAccepted, countryId, timezone);
        }


        private IObservable<ISyncManager> signUpWithGoogle(string googleToken, bool termsAccepted, int countryId, string timezone)
        {
            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User
                .SignUpWithGoogle(googleToken, termsAccepted, countryId, timezone)
                .Select(User.Clean)
                .SelectMany(database.User.Create)
                .Select(initializeFor)
                .Select(initializedServices => initializedServices.syncManager)
                .Do(_ => userLoggedInSubject.OnNext(Unit.Default));
        }
    }
}
