using CoreGraphics;
using Foundation;
using Toggl.Daneel.Extensions;
using UIKit;
using static Toggl.Foundation.MvvmCross.Helper.Animation;

namespace Toggl.Daneel.Presentation.Transition
{
    public sealed class FromRightTransition : NSObject, IUIViewControllerAnimatedTransitioning
    {
        private readonly bool presenting;

        public FromRightTransition(bool presenting)
        {
            this.presenting = presenting;
        }

        public double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            => presenting ? Timings.EnterTiming : Timings.LeaveTiming;

        public void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
        {
            var toController = transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey);
            var fromController = transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey);
            var animationDuration = TransitionDuration(transitionContext);

            if (presenting)
            {
                transitionContext.ContainerView.AddSubview(toController.View);

                var finalFrame = transitionContext.GetFinalFrameForViewController(toController);

                var frame = new CGRect(finalFrame.Location, finalFrame.Size);
                frame.Offset(transitionContext.ContainerView.Frame.Width - 20, 0.0f);
                toController.View.Frame = frame;
                toController.View.Alpha = 0.5f;

                AnimationExtensions.Animate(animationDuration, Curves.CardInCurve, () =>
                {
                    toController.View.Frame = finalFrame;
                    toController.View.Alpha = 1.0f;
                },
                () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
            }
            else
            {
                var initialFrame = transitionContext.GetInitialFrameForViewController(fromController);
                initialFrame.Offset(transitionContext.ContainerView.Frame.Width, 0.0f);
                var finalFrame = initialFrame;

                if (transitionContext.IsInteractive)
                {
                    UIView.Animate(
                        animationDuration,
                        () => fromController.View.Frame = finalFrame,
                        () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled)
                    );
                }
                else
                {
                    AnimationExtensions.Animate(animationDuration, Curves.CardOutCurve, () =>
                    {
                        fromController.View.Frame = finalFrame;
                        fromController.View.Alpha = 0.5f;
                    },
                    () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
                }
            }
        }
    }
}
