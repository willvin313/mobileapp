// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.WatchExtension.Cells
{
	[Register ("SuggestionRowController")]
	partial class SuggestionRowController
	{
		[Outlet]
		WatchKit.WKInterfaceGroup ContentGroup { get; set; }

		[Outlet]
		WatchKit.WKInterfaceLabel DescriptionLabel { get; set; }

		[Outlet]
		WatchKit.WKInterfaceLabel ProjectLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ContentGroup != null) {
				ContentGroup.Dispose ();
				ContentGroup = null;
			}

			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}

			if (ProjectLabel != null) {
				ProjectLabel.Dispose ();
				ProjectLabel = null;
			}
		}
	}
}
