using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Google.SignIn;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross;
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
        private Subject<GoogleAccountData> tokenSubject = new Subject<GoogleAccountData>();

        public void DidSignIn(SignIn signIn, GoogleUser user, NSError error)
        {
            if (error == null)
            {
                var token = user.Authentication.AccessToken;
                signIn.DisconnectUser();
                tokenSubject.OnNext(new GoogleAccountData(user.Profile.Name, token, user.Profile.Description));
                tokenSubject.OnCompleted();
            }
            else
            {
                tokenSubject.OnError(new GoogleLoginException(error.Code == cancelErrorCode));
            }

            tokenSubject = new Subject<GoogleAccountData>();
            loggingIn = false;
        }

        public IObservable<GoogleAccountData> GetGoogleAccountData()
        {
            if (!loggingIn)
            {
                SignIn.SharedInstance.Delegate = this;
                SignIn.SharedInstance.UIDelegate = this;
                try
                {
                    SignIn.SharedInstance.SignInUser();
                }
                catch (Exception e)
                {
                    return Observable.Throw<GoogleAccountData>(e);
                }
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
            var presenter = Mvx.Resolve<IMvxIosViewPresenter>() as MvxIosViewPresenter;
            presenter.MasterNavigationController.PresentViewController(viewController, true, null);
        }

        [Export("signIn:dismissViewController:")]
        public void DismissViewController(SignIn signIn, UIViewController viewController)
        {
            var presenter = Mvx.Resolve<IMvxIosViewPresenter>() as MvxIosViewPresenter;
            presenter.MasterNavigationController.DismissViewController(true, null);
        }
    }
}
