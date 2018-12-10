// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers.UserAccess
{
	[Register ("SignupOrLoginChoiceViewController")]
	partial class SignupOrLoginChoiceViewController
	{
		[Outlet]
		UIKit.UIButton LoginButton { get; set; }

		[Outlet]
		UIKit.UIButton SignUpButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (SignUpButton != null) {
				SignUpButton.Dispose ();
				SignUpButton = null;
			}

			if (LoginButton != null) {
				LoginButton.Dispose ();
				LoginButton = null;
			}
		}
	}
}
