// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.WatchExtension.InterfaceControllers
{
	[Register ("StartTimeEntryInterfaceController")]
	partial class StartTimeEntryInterfaceController
	{
		[Outlet]
		WatchKit.WKInterfaceButton EnterDescriptionButton { get; set; }

		[Outlet]
		WatchKit.WKInterfaceButton StartTimeEntryButton { get; set; }

		[Action ("OnEnterDescriptionPress:")]
		partial void OnEnterDescriptionPress (WatchKit.WKInterfaceButton sender);

		[Action ("OnStartTimeEntryPress:")]
		partial void OnStartTimeEntryPress (WatchKit.WKInterfaceButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (EnterDescriptionButton != null) {
				EnterDescriptionButton.Dispose ();
				EnterDescriptionButton = null;
			}

			if (StartTimeEntryButton != null) {
				StartTimeEntryButton.Dispose ();
				StartTimeEntryButton = null;
			}
		}
	}
}
