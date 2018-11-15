using System;

using WatchKit;
using Foundation;
using UIKit;

namespace Toggl.Daneel.WatchExtension.Cells
{
    public partial class SuggestionRowController : NSObject
    {
        protected SuggestionRowController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public void Configure(NSString description, NSString project, UIColor color)
        {
            DescriptionLabel.SetText(description);

            if (project != null && project.Length > 0)
            {
                ProjectLabel.SetText(project);
                ProjectLabel.SetHidden(false);
            }
            else
            {
                ProjectLabel.SetHidden(true);
            }
            ContentGroup.SetBackgroundColor(color);
        }
    }
}
