using System;
using System.Reactive.Disposables;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Cells.Reports
{
    public sealed partial class ReportsCalendarQuickSelectViewCell : BaseCollectionViewCell<QuickSelectShortcut>
    {
        public static readonly NSString Key = new NSString(nameof(ReportsCalendarQuickSelectViewCell));
        public static readonly UINib Nib;

        static ReportsCalendarQuickSelectViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarQuickSelectViewCell), NSBundle.MainBundle);
        }

        public ReportsCalendarQuickSelectViewCell(IntPtr handle)
            : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            TitleLabel.Font = UIFont.SystemFontOfSize(13, UIFontWeight.Medium);

            //Text
            TitleLabel.Text = Item.Title;

            //Color
            TitleLabel.TextColor = Item.TitleColor.ToNativeColor(); 
            ContentView.BackgroundColor =  Item.BackgroundColor.ToNativeColor();
        }
    }
}
