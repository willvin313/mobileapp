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
	[Register ("MainInterfaceController")]
	partial class MainInterfaceController
	{
		[Outlet]
		WatchKit.WKInterfaceLabel SuggestionsLabel { get; set; }

		[Outlet]
		WatchKit.WKInterfaceTable SuggestionsTable { get; set; }

		[Outlet]
		WatchKit.WKInterfaceLabel TimeEntriesLabel { get; set; }

		[Outlet]
		WatchKit.WKInterfaceTable TimeEntriesTable { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (SuggestionsLabel != null) {
				SuggestionsLabel.Dispose ();
				SuggestionsLabel = null;
			}

			if (SuggestionsTable != null) {
				SuggestionsTable.Dispose ();
				SuggestionsTable = null;
			}

			if (TimeEntriesLabel != null) {
				TimeEntriesLabel.Dispose ();
				TimeEntriesLabel = null;
			}

			if (TimeEntriesTable != null) {
				TimeEntriesTable.Dispose ();
				TimeEntriesTable = null;
			}
		}
	}
}
