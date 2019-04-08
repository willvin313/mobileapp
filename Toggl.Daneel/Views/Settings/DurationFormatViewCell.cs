﻿using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.UI.Transformations;
using Toggl.Foundation.UI.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views.Settings
{
    public sealed partial class DurationFormatViewCell : BaseTableViewCell<SelectableDurationFormatViewModel>
    {
        public static readonly string Identifier = nameof(DurationFormatViewCell);

        public static readonly NSString Key = new NSString(nameof(DurationFormatViewCell));
        public static readonly UINib Nib;

        static DurationFormatViewCell()
        {
            Nib = UINib.FromName(nameof(DurationFormatViewCell), NSBundle.MainBundle);
        }

        public DurationFormatViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            DurationFormatLabel.Text = string.Empty;
            SelectedImageView.Hidden = true;
        }

        protected override void UpdateView()
        {
            DurationFormatLabel.Text = DurationFormatToString.Convert(Item.DurationFormat);
            SelectedImageView.Hidden = !Item.Selected;
        }
    }
}
