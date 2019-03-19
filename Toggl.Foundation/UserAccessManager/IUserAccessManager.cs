using System;
using System.Reactive;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.Login
{
    public interface IUserAccessManager
    {
        IObservable<Unit> UserLoggedIn { get; }
        IObservable<Unit> UserLoggedOut { get; }

        void OnUserLoggedOut();

        bool TryInitializingAccessToUserData(out ISyncManager syncManager, out IInteractorFactory interactorFactory);

        IObservable<ISyncManager> LoginWithGoogle();
        IObservable<ISyncManager> Login(Email email, Password password);

        IObservable<ISyncManager> SignUpWithGoogle(bool termsAccepted, int countryId, string timezone);
        IObservable<ISyncManager> SignUp(Email email, Password password, bool termsAccepted, int countryId, string timezone);

        IObservable<ISyncManager> RefreshToken(Password password);

        IObservable<string> ResetPassword(Email email);
    }
}
