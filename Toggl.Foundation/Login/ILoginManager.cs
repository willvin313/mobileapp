using System;
using System.Reactive;
using Toggl.Multivac;

namespace Toggl.Foundation.Login
{
    public interface ILoginManager
    {
        bool IsUserLoggedIn();

        IObservable<bool> LoginWithGoogle();
        IObservable<bool> Login(Email email, Password password);

        IObservable<bool> SignUpWithGoogle(bool termsAccepted, int countryId);
        IObservable<bool> SignUp(Email email, Password password, bool termsAccepted, int countryId);

        void Logout();

        IObservable<bool> RefreshToken(Password password);

        IObservable<string> ResetPassword(Email email);

        IObservable<Unit> UserLoggedIn { get; }

        IObservable<Unit> UserLoggedOut { get; }
    }
}
