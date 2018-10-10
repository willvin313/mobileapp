using Foundation;
using MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    public abstract class KeyboardAwareViewController<TViewModel> : ReactiveViewController<TViewModel>
        where TViewModel : class, IMvxViewModel
    {
        private NSObject willShowNotification;
        private NSObject willHideNotification;

        protected KeyboardAwareViewController(string nibName)
            : base(nibName) { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            willShowNotification = UIKeyboard.Notifications.ObserveWillShow(KeyboardWillShow);
            willHideNotification = UIKeyboard.Notifications.ObserveWillHide(KeyboardWillHide);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            NSNotificationCenter.DefaultCenter.RemoveObserver(willShowNotification);
            NSNotificationCenter.DefaultCenter.RemoveObserver(willHideNotification);
        }

        protected abstract void KeyboardWillShow(object sender, UIKeyboardEventArgs e);

        protected abstract void KeyboardWillHide(object sender, UIKeyboardEventArgs e);
    }
}
