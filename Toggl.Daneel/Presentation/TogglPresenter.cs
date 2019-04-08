﻿using System;
using System.Collections.Generic;
using System.Linq;
using CoreAnimation;
using MvvmCross.ViewModels;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Presentation.Transition;
using Toggl.Daneel.Services;
using Toggl.Daneel.ViewControllers;
using Toggl.Daneel.ViewControllers.Navigation;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.UI.ViewModels.Hints;
using UIKit;
using MvvmCross.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.Presenters;
using Toggl.Foundation.UI.ViewModels.Calendar;
using Toggl.Foundation.UI.ViewModels.Reports;

namespace Toggl.Daneel.Presentation
{
    public sealed class TogglPresenter : MvxIosViewPresenter, ITopViewControllerProvider
    {
        private readonly ModalTransitionDelegate modalTransitionDelegate = new ModalTransitionDelegate();

        private readonly Dictionary<Type, INestedPresentationInfo> nestedPresentationInfo;
        private readonly List<Type> tabViewModelTypes = new List<Type>
        {
            typeof(MainViewModel),
            typeof(ReportsViewModel),
            typeof(CalendarViewModel)
        };

        private readonly CATransition fadeAnimation = new CATransition
        {
            Duration = Animation.Timings.EnterTiming,
            Type = CAAnimation.TransitionFade,
            Subtype = CAAnimation.TransitionFromTop,
            TimingFunction = Animation.Curves.SharpCurve.ToMediaTimingFunction()
        };

        private UITabBarController mainTabBarController
            => mainViewController as UITabBarController;

        private UIViewController mainViewController
            => UIApplication.SharedApplication.KeyWindow?.RootViewController;

        public UIViewController TopViewController
            => getPresentedViewController(mainViewController);

        public TogglPresenter(IUIApplicationDelegate applicationDelegate, UIWindow window)
            : base(applicationDelegate, window)
        {
            nestedPresentationInfo = createNestedPresentationInfo();
        }

        public override void RegisterAttributeTypes()
        {
            base.RegisterAttributeTypes();

            AttributeTypesToActionsDictionary.Add(
                typeof(NestedPresentationAttribute),
                new MvxPresentationAttributeAction
                {
                    ShowAction = showNestedViewController,
                    CloseAction = (viewModel, attribute) => false
                });

            AttributeTypesToActionsDictionary.Add(
                typeof(ModalCardPresentationAttribute),
                new MvxPresentationAttributeAction
                {
                    ShowAction = showModalCardViewController,
                    CloseAction = (viewModel, attribute) => CloseModalViewController(viewModel, (MvxModalPresentationAttribute)attribute)
                });

            AttributeTypesToActionsDictionary.Add(
                typeof(ModalDialogPresentationAttribute),
                new MvxPresentationAttributeAction
                {
                    ShowAction = showModalDialogViewController,
                    CloseAction = (viewModel, attribute) => CloseModalViewController(viewModel, (MvxModalPresentationAttribute)attribute)
                });

            AttributeTypesToActionsDictionary.Add(
                typeof(TabPresentationAttribute),
                new MvxPresentationAttributeAction
                {
                    ShowAction = showTabViewController,
                    CloseAction = (viewModel, attribute) => false
                });
        }

        private void showTabViewController(Type viewType, MvxBasePresentationAttribute attribute, MvxViewModelRequest request)
        {
            var tabIndex = tabViewModelTypes.IndexOf(request.ViewModelType);
            if (tabIndex < 0)
                return;

            mainTabBarController.SelectedIndex = tabIndex;

            if (mainTabBarController.ViewControllers[tabIndex] is UINavigationController navigationController)
            {
                navigationController.PopToRootViewController(false);
            }
        }

        private void showNestedViewController(Type viewType, MvxBasePresentationAttribute attribute, MvxViewModelRequest request)
        {
            var presentationInfo = nestedPresentationInfo[viewType];
            var parentViewController = presentationInfo.ViewController;
            var containerView = presentationInfo.Container;
            var viewController = (UIViewController)this.CreateViewControllerFor(request);

            parentViewController.AddChildViewController(viewController);
            containerView.AddSubview(viewController.View);

            viewController.View.TopAnchor.ConstraintEqualTo(containerView.TopAnchor).Active = true;
            viewController.View.BottomAnchor.ConstraintEqualTo(containerView.BottomAnchor).Active = true;
            viewController.View.LeftAnchor.ConstraintEqualTo(containerView.LeftAnchor).Active = true;
            viewController.View.RightAnchor.ConstraintEqualTo(containerView.RightAnchor).Active = true;
            viewController.View.TranslatesAutoresizingMaskIntoConstraints = false;

            viewController.DidMoveToParentViewController(parentViewController);
        }

        private void showModalCardViewController(Type viewType, MvxBasePresentationAttribute attribute, MvxViewModelRequest request)
        {
            var viewController = (UIViewController)this.CreateViewControllerFor(request);
            var transitionDelegate = new FromBottomTransitionDelegate(
                () => ModalViewControllers.Remove(viewController)
            );

            viewController.ModalPresentationStyle = UIModalPresentationStyle.Custom;
            viewController.TransitioningDelegate = transitionDelegate;

            TopViewController.PresentViewController(viewController, true, null);

            ModalViewControllers.Add(viewController);
        }

        private void showModalDialogViewController(Type viewType, MvxBasePresentationAttribute attribute, MvxViewModelRequest request)
        {
            var viewController = (UIViewController)this.CreateViewControllerFor(request);
            viewController.ModalPresentationStyle = UIModalPresentationStyle.Custom;
            viewController.TransitioningDelegate = modalTransitionDelegate;

            TopViewController.PresentViewController(viewController, true, null);

            ModalViewControllers.Add(viewController);
        }

        protected override void ShowChildViewController(UIViewController viewController, MvxChildPresentationAttribute attribute, MvxViewModelRequest request)
        {
            if (request.ViewModelType == typeof(LoginViewModel))
            {
                MasterNavigationController.View.Layer.AddAnimation(fadeAnimation, CALayer.Transition);
                MasterNavigationController.PushViewController(viewController, false);
                return;
            }

            base.ShowChildViewController(viewController, attribute, request);
        }

        protected override void SetWindowRootViewController(UIViewController controller, MvxRootPresentationAttribute attribute = null)
        {
            UIView.Transition(
                _window,
                Animation.Timings.EnterTiming,
                UIViewAnimationOptions.TransitionCrossDissolve,
                () => _window.RootViewController = controller,
                null
            );
        }

        public override void Show(MvxViewModelRequest request)
        {
            var topViewController = mainTabBarController == null
                ? null
                : getPresentedViewController(mainTabBarController.SelectedViewController) as MvxViewController;

            //Don't show the same view twice
            if (topViewController?.ViewModel?.GetType() == request.ViewModelType)
                return;

            base.Show(request);
        }

        public override void Close(IMvxViewModel viewModel)
        {
            if (viewModel is LoginViewModel)
            {
                MasterNavigationController.View.Window.Layer.AddAnimation(fadeAnimation, CALayer.Transition);
                MasterNavigationController.PopViewController(false);
                return;
            }

            base.Close(viewModel);
        }

        protected override MvxNavigationController CreateNavigationController(UIViewController viewController)
        {
            if (viewController is OnboardingViewController || viewController is TokenResetViewController)
                return new OnboardingFlowNavigationController(viewController);

            return base.CreateNavigationController(viewController);
        }

        protected override bool CloseModalViewController(IMvxViewModel toClose, MvxModalPresentationAttribute attribute)
        {
            var closeDidWork = base.CloseModalViewController(toClose, attribute);
            if (closeDidWork)
            {
                toClose?.ViewDestroy(true);
            }

            return closeDidWork;
        }

        public override void ChangePresentation(MvxPresentationHint hint)
        {
            switch (hint)
            {
                case ToggleReportsCalendarVisibilityHint calendarHint:

                    if ((mainTabBarController.SelectedViewController as UINavigationController).TopViewController is ReportsViewController reportsViewController)
                    {
                        if (calendarHint.ForceHide || reportsViewController.CalendarIsVisible)
                        {
                            reportsViewController.HideCalendar();
                        }
                        else
                        {
                            reportsViewController.ShowCalendar();
                        }
                    }
                    return;
            }

            base.ChangePresentation(hint);
        }

        private UIViewController getPresentedViewController(UIViewController current)
            => current.PresentedViewController == null || current.PresentedViewController.IsBeingDismissed
            ? current
            : getPresentedViewController(current.PresentedViewController);

        private T findViewController<T>()
        {
            var tabControllers = mainTabBarController.ViewControllers;
            var viewControllers = new List<UIViewController>();
            foreach (UINavigationController nav in tabControllers)
            {
                viewControllers.AddRange(nav.ViewControllers);
            }
            return viewControllers.AsEnumerable().OfType<T>().Single();
        }

        private Dictionary<Type, INestedPresentationInfo> createNestedPresentationInfo()
            => new Dictionary<Type, INestedPresentationInfo>
            {
                {
                    typeof(ReportsCalendarViewController),
                    new NestedPresentationInfo<ReportsViewController>(
                        () => findViewController<ReportsViewController>(),
                        reportsController => reportsController.CalendarContainerView)
                }
            };
    }
}
