using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Google.SignIn;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Login;
using UIKit;

namespace Toggl.Daneel.Services
{
    [MvvmCross.Preserve(AllMembers = true)]
    public sealed class GoogleServiceIos : NSObject, IGoogleService, ISignInDelegate, ISignInUIDelegate
    {
        private const int cancelErrorCode = -5;

        private bool loggingIn;
        private Subject<string> tokenSubject = new Subject<string>();

        public void DidSignIn(SignIn signIn, GoogleUser user, NSError error)
        {
            if (error == null)
            {
                var token = user.Authentication.AccessToken;
                signIn.DisconnectUser();
                tokenSubject.OnNext(token);
            }
            else
            {
                tokenSubject.OnError(new GoogleLoginException(error.Code == cancelErrorCode));
            }

            tokenSubject.OnCompleted();

            tokenSubject = new Subject<string>();
            loggingIn = false;
        }

        public IObservable<string> GetAuthToken()
        {
            if (!loggingIn)
            {
                SignIn.SharedInstance.Delegate = this;
                SignIn.SharedInstance.UIDelegate = this;
                SignIn.SharedInstance.SignInUser();
                loggingIn = true;
            }

            return tokenSubject.AsObservable();
        }

        public IObservable<Unit> LogOutIfNeeded()
        {
            if (SignIn.SharedInstance.CurrentUser != null)
            {
                SignIn.SharedInstance.SignOutUser();
            }

            return Observable.Return(Unit.Default);
        }

        [Export("signIn:presentViewController:")]
        public void PresentViewController(SignIn signIn, UIViewController viewController)
        {
            IosDependencyContainer.Instance
                .ViewPresenter
                .MasterNavigationController
                .PresentViewController(viewController, true, null);
        }

        [Export("signIn:dismissViewController:")]
        public void DismissViewController(SignIn signIn, UIViewController viewController)
        {
            IosDependencyContainer.Instance
                .ViewPresenter
                .MasterNavigationController
                .DismissViewController(true, null);
        }
    }
}
