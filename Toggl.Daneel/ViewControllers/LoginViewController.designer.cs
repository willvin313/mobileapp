// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
    [Register ("LoginView")]
    partial class LoginViewController
    {
        [Outlet]
        Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicator { get; set; }

        [Outlet]
        UIKit.UIView FirstScreenWrapperView { get; set; }

        [Outlet]
        UIKit.UIButton GoogleLoginButton { get; set; }

        [Outlet]
        UIKit.UIButton LoginWithEmailButton { get; set; }

        [Outlet]
        UIKit.UILabel LoginWithEmailErrorLabel { get; set; }

        [Outlet]
        Toggl.Daneel.Views.LoginTextField LoginWithEmailTextField { get; set; }

        [Outlet]
        UIKit.UIView SecondScreenWrapperView { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (ActivityIndicator != null) {
                ActivityIndicator.Dispose ();
                ActivityIndicator = null;
            }

            if (GoogleLoginButton != null) {
                GoogleLoginButton.Dispose ();
                GoogleLoginButton = null;
            }

            if (LoginWithEmailButton != null) {
                LoginWithEmailButton.Dispose ();
                LoginWithEmailButton = null;
            }

            if (LoginWithEmailErrorLabel != null) {
                LoginWithEmailErrorLabel.Dispose ();
                LoginWithEmailErrorLabel = null;
            }

            if (LoginWithEmailTextField != null) {
                LoginWithEmailTextField.Dispose ();
                LoginWithEmailTextField = null;
            }

            if (FirstScreenWrapperView != null) {
                FirstScreenWrapperView.Dispose ();
                FirstScreenWrapperView = null;
            }

            if (SecondScreenWrapperView != null) {
                SecondScreenWrapperView.Dispose ();
                SecondScreenWrapperView = null;
            }
        }
    }
}
