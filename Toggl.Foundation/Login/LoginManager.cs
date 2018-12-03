using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Login
{
    public sealed class LoginManager : ILoginManager
    {
        private readonly IApiFactory apiFactory;
        private readonly ITogglDatabase database;
        private readonly IGoogleService googleService;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly ISubject<Unit> userLoggedInSubject = new Subject<Unit>();
        private readonly ISubject<Unit> userLoggedOutSubject = new Subject<Unit>();

        public IObservable<Unit> UserLoggedIn { get; }
        public IObservable<Unit> UserLoggedOut { get; }

        public LoginManager(
            IApiFactory apiFactory,
            ITogglDatabase database,
            IGoogleService googleService,
            IPrivateSharedStorageService privateSharedStorageService)
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(googleService, nameof(googleService));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));

            this.database = database;
            this.apiFactory = apiFactory;
            this.googleService = googleService;
            this.privateSharedStorageService = privateSharedStorageService;

            UserLoggedIn = userLoggedInSubject.AsObservable();
            UserLoggedOut = userLoggedOutSubject.AsObservable();
        }

        public bool IsUserLoggedIn()
            => database.User
                    .Single()
                    .SelectValue(true)
                    .Catch(Observable.Return(false))
                    .Wait();

        public IObservable<bool> Login(Email email, Password password)
            => Observable.StartAsync(async () =>
            {
                if (!email.IsValid)
                    throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to login");
                if (!password.IsValid)
                    throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to login");

                var credentials = Credentials.WithPassword(email, password);

                await database.Clear();

                var user = await apiFactory
                    .CreateApiWith(credentials)
                    .User
                    .Get()
                    .Select(User.Clean)
                    .SelectMany(database.User.Create);

                finishLogin(user);
                return true;
            });

        public IObservable<bool> LoginWithGoogle()
            => Observable.StartAsync(async () =>
            {
                await database.Clear();

                await googleService.LogOutIfNeeded();

                var authToken = await googleService.GetAuthToken();
                var credentials = Credentials.WithGoogleToken(authToken);

                var user = await apiFactory.CreateApiWith(credentials)
                    .User.GetWithGoogle()
                    .Select(User.Clean)
                    .SelectMany(database.User.Create);

                finishLogin(user);
                return true;
            });

        public IObservable<bool> SignUp(Email email, Password password, bool termsAccepted, int countryId)
            => Observable.StartAsync(async () =>
            {
                if (!email.IsValid)
                    throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to signup");
                if (!password.IsValid)
                    throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to signup");

                await database.Clear();

                var user = await apiFactory
                    .CreateApiWith(Credentials.None)
                    .User.SignUp(email, password, termsAccepted, countryId)
                    .Select(User.Clean)
                    .SelectMany(database.User.Create);

                finishLogin(user);
                return true;
            });

        public IObservable<bool> SignUpWithGoogle(bool termsAccepted, int countryId)

            => Observable.StartAsync(async () =>
            {
                await database.Clear();

                await googleService.LogOutIfNeeded();
                var googleToken = await googleService.GetAuthToken();

                var user = await apiFactory
                        .CreateApiWith(Credentials.None)
                        .User.SignUpWithGoogle(googleToken, termsAccepted, countryId)
                        .Select(User.Clean)
                        .SelectMany(database.User.Create);

                finishLogin(user);
                return true;
            });

        public IObservable<bool> RefreshToken(Password password)
            => Observable.StartAsync(async () =>
            {
                if (!password.IsValid)
                    throw new ArgumentException($"A valid {nameof(password)} must be provided when trying to refresh token");

                var email = await database.User.Single().Select(u => u.Email);

                var credentials = Credentials.WithPassword(email, password);

                var user = await apiFactory
                    .CreateApiWith(credentials)
                    .User.Get()
                    .Select(User.Clean)
                    .SelectMany(database.User.Update);

                finishLogin(user);
                return true;
            });

        public IObservable<string> ResetPassword(Email email)
        {
            if (!email.IsValid)
                throw new ArgumentException($"A valid {nameof(email)} must be provided when trying to reset forgotten password.");

            var api = apiFactory.CreateApiWith(Credentials.None);
            return api.User.ResetPassword(email);
        }

        public void Logout()
            => userLoggedOutSubject.OnNext(Unit.Default);

        private void finishLogin(IUser user)
        {
            privateSharedStorageService.SaveApiToken(user.ApiToken);
            privateSharedStorageService.SaveUserId(user.Id);
            userLoggedInSubject.OnNext(Unit.Default);
        }
    }
}
