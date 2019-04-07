using System;
using System.Linq;
using System.Reactive.Linq;
using Foundation;
using Social;
using Toggl.Daneel.ExtensionKit;
using Toggl.Daneel.ExtensionKit.Models;
using Toggl.Daneel.ShareExtension.Helper;
using Toggl.Ultrawave;

namespace Toggl.Daneel.ShareExtension
{
    public partial class ShareViewController : SLComposeServiceViewController
    {
        private const int maximumEntryLength = 3000;
        private ITogglApi togglAPI = APIHelper.GetTogglAPI();

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
            var lastUpdated = SharedStorage.instance.GetLastUpdateDate();

            togglAPI.TimeEntries.GetAllSince(lastUpdated)
                .Select(tes =>
                    {
                        // If there are no changes since last sync, or there are changes in the server but not in the app, we are ok
                        if (tes.Count == 0 || tes.OrderBy(te => te.At).Last().At >= lastUpdated)
                        {
                            return tes;
                        }

                        throw new Exception("Sync conflict, open the app.");
                    }
                )
                .SelectMany(te =>
                {
                    return togglAPI.User
                        .Get()
                        .Select(user =>
                        {
                            if (user.DefaultWorkspaceId == null)
                            {
                                throw new Exception("Something went wrong");
                            }

                            return (long) user.DefaultWorkspaceId;
                        });
                })
                .SelectMany(workspaceId =>
                {
                    return togglAPI.TimeEntries.Create(
                        new TimeEntry(
                            workspaceId,
                            null,
                            null,
                            false,
                            DateTimeOffset.Now,
                            null,
                            ContentText,
                            new long[0],
                            (long) SharedStorage.instance.GetUserId(),
                            0,
                            null,
                            DateTimeOffset.Now)
                    );
                })
                .Subscribe(te =>
                    {
                        SharedStorage.instance.SetNeedsSync(true);
                        ExtensionContext.CompleteRequest(new NSExtensionItem[0], null);
                    },
                    exception =>
                    {
                        Console.WriteLine(exception);
                    });
        }
    }
}
