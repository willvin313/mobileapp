using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Auth;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Views.Base;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Login;

namespace Toggl.Giskard.Services
{
    public sealed class GoogleServiceAndroid : MvxAndroidTask, IGoogleService
    {
        private readonly object lockable = new object();

        private bool isLoggingIn;
        private Subject<GoogleAccountData> loginSubject = new Subject<GoogleAccountData>();
        private Subject<Unit> logoutSubject = new Subject<Unit>();
        private readonly string scope = $"oauth2:{Scopes.Profile}";

        private GoogleApiClient googleApiClient;

        public GoogleServiceAndroid()
        {
            var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .Build();

            googleApiClient = new GoogleApiClient.Builder(Application.Context)
                .AddConnectionCallbacks(login)
                .AddOnConnectionFailedListener(onError)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, signInOptions)
                .Build();
        }

        public IObservable<Unit> LogOutIfNeeded()
        {
            logoutSubject = new Subject<Unit>();

            if (googleApiClient.IsConnected)
            {
                var logoutCallback = new LogOutCallback(() =>
                {
                    logoutSubject.OnNext(Unit.Default);
                    logoutSubject.OnCompleted();
                });
                Auth.GoogleSignInApi.SignOut(googleApiClient).SetResultCallback(logoutCallback);
                return logoutSubject.AsObservable();
            }
            else
            {
                return Observable.Return(Unit.Default);
            }
        }

        public IObservable<GoogleAccountData> GetGoogleAccountData()
        {
            lock (lockable)
            {
                if (isLoggingIn)
                    return loginSubject.AsObservable();

                isLoggingIn = true;
                loginSubject = new Subject<GoogleAccountData>();

                if (googleApiClient.IsConnected)
                {
                    login();
                }
                else
                {
                    googleApiClient.Connect();
                }

                return loginSubject.AsObservable();
            }
        }

        protected override void ProcessMvxIntentResult(MvxIntentResultEventArgs result)
        {
            base.ProcessMvxIntentResult(result);

            if (result.RequestCode != IntentResultRequestCodes.SignIn)
                return;

            lock (lockable)
            {
                var signInData = Auth.GoogleSignInApi.GetSignInResultFromIntent(result.Data);
                if (signInData.IsSuccess)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var account = signInData.SignInAccount;
                            var token = GoogleAuthUtil.GetToken(Application.Context, signInData.SignInAccount.Account, scope);
                            loginSubject.OnNext(new GoogleAccountData(account.GivenName + " " + account.FamilyName, token, account.Email));
                            loginSubject.OnCompleted();
                        }
                        catch (Exception e)
                        {
                            loginSubject.OnError(e);
                        }
                        finally
                        {
                            isLoggingIn = false;
                        }
                    });
                }
                else
                {
                    loginSubject.OnError(new GoogleLoginException(signInData.Status.IsCanceled));
                    isLoggingIn = false;
                }
            }
        }

        private void login()
        {
            if (!isLoggingIn) return;

            if (!googleApiClient.IsConnected)
            {
                throw new GoogleLoginException(false);
            }

            var intent = Auth.GoogleSignInApi.GetSignInIntent(googleApiClient);
            StartActivityForResult(IntentResultRequestCodes.SignIn, intent);
        }

        private void onError(ConnectionResult result)
        {
            lock (lockable)
            {
                loginSubject.OnError(new GoogleLoginException(false));
                isLoggingIn = false;
            }
        }

        private class LogOutCallback : Java.Lang.Object, IResultCallback
        {
            private Action callback;

            public LogOutCallback(Action callback)
            {
                this.callback = callback;
            }

            public void OnResult(Java.Lang.Object result)
            {
                callback();
            }
        }
    }
}
