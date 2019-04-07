using System;

using Foundation;
using Social;

namespace Toggl.Daneel.ShareExtension
{
    public partial class ShareViewController : SLComposeServiceViewController
    {
        private const int maximumEntryLength = 3000;
        protected ShareViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            NavigationController.NavigationBar.TopItem.RightBarButtonItem.Title = "Track";
        }

        public override bool IsContentValid()
        {
            var remainingCharLength = maximumEntryLength - ContentText.Length;
            CharactersRemaining = new NSNumber(remainingCharLength);
            return remainingCharLength > 0;
        }

        public override void DidSelectPost()
        {
            // This is called after the user selects Post. Do the upload of contentText and/or NSExtensionContext attachments.

            // Inform the host that we're done, so it un-blocks its UI. Note: Alternatively you could call super's -didSelectPost, which will similarly complete the extension context.
            ExtensionContext.CompleteRequest(new NSExtensionItem[0], null);
        }

        public override SLComposeSheetConfigurationItem[] GetConfigurationItems()
        {
            // To add configuration options via table cells at the bottom of the sheet, return an array of SLComposeSheetConfigurationItem here.
            return new SLComposeSheetConfigurationItem[0];
        }
    }
}
