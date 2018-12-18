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
	[Register ("SignupViewController")]
	partial class SignupViewController
	{
		[Outlet]
		UIKit.NSLayoutConstraint BottomToSafeAreaConstraint { get; set; }

		[Outlet]
		UIKit.UIView CountrySelectionScreenWrapperView { get; set; }

		[Outlet]
		UIKit.UIView EmailAndPasswordScreenWrapperView { get; set; }

		[Outlet]
		UIKit.UILabel EmailScreenErrorLabel { get; set; }

		[Outlet]
		UIKit.UIView EmailScreenWrapperView { get; set; }

		[Outlet]
		UIKit.UIButton GoogleSignUpButton { get; set; }

		[Outlet]
		UIKit.UIButton SignUpWithEmailButton { get; set; }

		[Outlet]
		Toggl.Daneel.Views.LoginTextField SignUpWithEmailTextField { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (BottomToSafeAreaConstraint != null) {
				BottomToSafeAreaConstraint.Dispose ();
				BottomToSafeAreaConstraint = null;
			}

			if (CountrySelectionScreenWrapperView != null) {
				CountrySelectionScreenWrapperView.Dispose ();
				CountrySelectionScreenWrapperView = null;
			}

			if (EmailAndPasswordScreenWrapperView != null) {
				EmailAndPasswordScreenWrapperView.Dispose ();
				EmailAndPasswordScreenWrapperView = null;
			}

			if (EmailScreenWrapperView != null) {
				EmailScreenWrapperView.Dispose ();
				EmailScreenWrapperView = null;
			}

			if (EmailScreenErrorLabel != null) {
				EmailScreenErrorLabel.Dispose ();
				EmailScreenErrorLabel = null;
			}

			if (GoogleSignUpButton != null) {
				GoogleSignUpButton.Dispose ();
				GoogleSignUpButton = null;
			}

			if (SignUpWithEmailButton != null) {
				SignUpWithEmailButton.Dispose ();
				SignUpWithEmailButton = null;
			}

			if (SignUpWithEmailTextField != null) {
				SignUpWithEmailTextField.Dispose ();
				SignUpWithEmailTextField = null;
			}
		}
	}
}
