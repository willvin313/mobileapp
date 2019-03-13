using System;
using System.Reactive;
using Toggl.Multivac;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Login
{
    public interface IUserAccessManager
    {
        IObservable<ITogglApi> UserLoggedIn { get; }
        IObservable<Unit> UserLoggedOut { get; }

        void OnUserLoggedOut();

        bool CheckIfLoggedIn();

        IObservable<Unit> LoginWithGoogle();
        IObservable<Unit> Login(Email email, Password password);

        IObservable<Unit> SignUpWithGoogle(bool termsAccepted, int countryId, string timezone);
        IObservable<Unit> SignUp(Email email, Password password, bool termsAccepted, int countryId, string timezone);

        IObservable<Unit> RefreshToken(Password password);

        IObservable<string> ResetPassword(Email email);
    }
}
