using System;
using Foundation;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Presentation.Transition
{
    public sealed class FromRightTransitionDelegate : NSObject, IUIViewControllerTransitioningDelegate
    {
        private readonly Action onDismissedCallback;

        public FromRightTransitionDelegate(Action onDismissedCallback)
        {
            Ensure.Argument.IsNotNull(onDismissedCallback, nameof(onDismissedCallback));

            this.onDismissedCallback = onDismissedCallback;
        }

        [Export("animationControllerForPresentedController:presentingController:sourceController:")]
        public IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(
            UIViewController presented, UIViewController presenting, UIViewController source
        ) => new FromRightTransition(true);

        [Export("animationControllerForDismissedController:")]
        public IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
            => new FromRightTransition(false);

        [Export("presentationControllerForPresentedViewController:presentingViewController:sourceViewController:")]
        public UIPresentationController GetPresentationControllerForPresentedViewController(
            UIViewController presented, UIViewController presenting, UIViewController source
        ) => new SplitScreenPresentationController(presented, presenting, onDismissedCallback);
    }
}
