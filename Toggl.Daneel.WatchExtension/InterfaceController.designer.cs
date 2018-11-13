// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.WatchExtension
{
	[Register ("InterfaceController")]
	partial class InterfaceController
	{
		[Outlet]
		WatchKit.WKInterfaceLabel DescriptionLabel { get; set; }

		[Outlet]
		WatchKit.WKInterfaceTimer RunningTimer { get; set; }

		[Outlet]
		WatchKit.WKInterfaceButton StopButton { get; set; }

		[Action ("OnStopButtonPress:")]
		partial void OnStopButtonPress (WatchKit.WKInterfaceButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}

			if (RunningTimer != null) {
				RunningTimer.Dispose ();
				RunningTimer = null;
			}

			if (StopButton != null) {
				StopButton.Dispose ();
				StopButton = null;
			}
		}
	}
}
