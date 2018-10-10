using System;
using UIKit;
using Foundation;

namespace Toggl.Daneel.SiriExtension.UI
{
    [Register("ConfirmationView")]
    public partial class ConfirmationView : UIView
    {
        public UILabel ConfirmationLabel => confirmationLabel;

        public ConfirmationView(IntPtr p) : base(p)
        {
            //Your initialization code.
        }
    }
}
