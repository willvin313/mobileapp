using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = false)]
    public class MainTabBarController : MvxTabBarViewController<MainTabBarViewModel>
    {
        private static readonly Dictionary<Type, string> imageNameForType = new Dictionary<Type, string>
        {
            { typeof(MainViewModel), "icTime" },
            { typeof(ReportsViewModel), "icReports" },
            { typeof(CalendarViewModel), "icCalendar" },
            { typeof(SettingsViewModel), "icSettings" }
        };
        
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            
            ViewControllers = ViewModel.Tabs.Select(createTabFor).ToArray();
            TabBar.Translucent = UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

            UIViewController createTabFor(IMvxViewModel viewModel)
            {
                var viewController = this.CreateViewControllerFor(viewModel) as UIViewController;
                viewController.TabBarItem = new UITabBarItem
                {
                    Title = "",
                    Image = UIImage.FromBundle(imageNameForType[viewModel.GetType()]),
                    ImageInsets = new UIEdgeInsets(6, 0, -6, 0)
                };
                return new UINavigationController(viewController);
            }
        }

        public override void ItemSelected(UITabBar tabbar, UITabBarItem item)
        {
            var targetViewController = ViewControllers.Single(vc => vc.TabBarItem == item);
            if (targetViewController is UINavigationController navigationController
                && navigationController.TopViewController is ReportsViewController)
            {
                ViewModel.StartReportsStopwatch();
            }
        }
    }
}
