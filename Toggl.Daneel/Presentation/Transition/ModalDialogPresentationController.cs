using CoreGraphics;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Presentation.Transition
{
    public class ModalDialogPresentationController : UIPresentationController
    {
        private readonly UIView dimmingView = new UIView
        {
            BackgroundColor = Color.ModalDialog.BackgroundOverlay.ToNativeColor(),
            Alpha = 0
        };

        public ModalDialogPresentationController(UIViewController presentedViewController, UIViewController presentingViewController)
            : base(presentedViewController, presentingViewController)
        {

        }

        public override void PresentationTransitionWillBegin()
        {
            dimmingView.Frame = ContainerView.Bounds;
            ContainerView.AddSubview(dimmingView);

            var transitionCoordinator = PresentingViewController.GetTransitionCoordinator();
            transitionCoordinator.AnimateAlongsideTransition(context => dimmingView.Alpha = 0.8f, null);
        }

        public override void DismissalTransitionWillBegin()
        {
            var transitionCoordinator = PresentingViewController.GetTransitionCoordinator();
            transitionCoordinator.AnimateAlongsideTransition(context => dimmingView.Alpha = 0.0f, null);
        }

        public override void ContainerViewWillLayoutSubviews()
        {
            PresentedView.Layer.CornerRadius = 8;
            dimmingView.Frame = ContainerView.Bounds;
        }

        public override CGSize GetSizeForChildContentContainer(IUIContentContainer contentContainer, CGSize parentContainerSize)
        {
            if (PresentingViewController.TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular
                    && PresentingViewController.TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                var largerSide = System.Math.Max(parentContainerSize.Width, parentContainerSize.Height);
                var preferredSize = PresentedViewController.PreferredContentSize;
                return new CGSize((largerSide / 2) - 32, preferredSize.Height); // 16 point padding
            }
            else
            {
                return PresentedViewController.PreferredContentSize;
            }
        }

        public override CGRect FrameOfPresentedViewInContainerView
        {
            get
            {
                var containerSize = ContainerView.Bounds.Size;
                var frame = CGRect.Empty;
                frame.Size = GetSizeForChildContentContainer(PresentedViewController, containerSize);
                frame.X = (containerSize.Width - frame.Width) / 2;
                frame.Y = (containerSize.Height - frame.Height) / 2;
                return frame;
            }
        }
    }
}
