using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AgileBits;
using Foundation;
using MvvmCross;
using MvvmCross.Platforms.Ios.Presenters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using LoginHandler = AgileBits.OnePasswordLoginDictionaryCompletionBlock;

namespace Toggl.Daneel.Services
{
    public sealed class OnePasswordServiceIos : NSObject, IPasswordManagerService
    {
        public bool IsAvailable => OnePasswordExtension.SharedExtension.IsAppExtensionAvailable;

        public IObservable<PasswordManagerResult> GetLoginInformation()
        {
            return Observable.Create<PasswordManagerResult>(observer =>
            {
                var presenter = IosDependencyContainer.Instance.ViewPresenter;

                OnePasswordExtension.SharedExtension.FindLoginForURLString(
                    "https://www.toggl.com", 
                    presenter.MasterNavigationController, 
                    this, 
                    getOnePasswordHandler(observer)
                );

                return Disposable.Empty;
            });
        }

        private LoginHandler getOnePasswordHandler(IObserver<PasswordManagerResult> observer) => (dict, ex) =>
        {
            if (dict != null && dict.Any())
            {
                var email = dict[AppExtensionLoginDictionarykeys.UsernameKey] as NSString;
                var password = dict[AppExtensionLoginDictionarykeys.PasswordKey] as NSString;

                observer.OnNext(new PasswordManagerResult(
                    Email.From(email), Password.From(password)));
            }
            else
            {
                observer.OnNext(PasswordManagerResult.None);
            }

            observer.OnCompleted();
        };
    }
}
