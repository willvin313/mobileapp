using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.UI.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views.Client
{
    public partial class ClientViewCell : BaseTableViewCell<SelectableClientBaseViewModel>
    {
        public static readonly string Identifier = nameof(ClientViewCell);
        public static readonly UINib Nib;

        static ClientViewCell()
        {
            Nib = UINib.FromName(nameof(ClientViewCell), NSBundle.MainBundle);
        }

        protected ClientViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            NameLabel.Text = Item.Name;
        }
    }
}