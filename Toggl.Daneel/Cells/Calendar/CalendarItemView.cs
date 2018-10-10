using System;
using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.Calendar;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Extensions;
using UIKit;

namespace Toggl.Daneel.Cells.Calendar
{
    public sealed partial class CalendarItemView : ReactiveCollectionViewCell<CalendarItem>
    {
        private static readonly Dictionary<CalendarIconKind, UIImage> images;

        private CAShapeLayer topDragIndicatorBorderLayer;
        private CAShapeLayer bottomDragIndicatorBorderLayer;

        public static readonly NSString Key = new NSString(nameof(CalendarItemView));
        public static readonly UINib Nib;

        public CGRect TopDragTouchArea => TopDragIndicator.Frame.Inset(-20, -20);
        public CGRect BottomDragTouchArea => BottomDragIndicator.Frame.Inset(-20, -20);

        //In other words: 30 minutes or less
        private bool shortCalendarItem => Frame.Height <= CalendarCollectionViewLayout.HourHeight / 2;

        private bool shouldCenterIconVertically => Frame.Height <= CalendarCollectionViewLayout.HourHeight / 4;

        private bool isEditing;
        public bool IsEditing
        {
            get => isEditing;
            set
            {
                isEditing = value;
                updateDragIndicators(itemColor());
                updateShadow();
            }
        }

        static CalendarItemView()
        {
            Nib = UINib.FromName(nameof(CalendarItemView), NSBundle.MainBundle);

            images = new Dictionary<CalendarIconKind, UIImage>
            {
                { CalendarIconKind.Unsynced, templateImage("icUnsynced") },
                { CalendarIconKind.Event, templateImage("icCalendarSmall") },
                { CalendarIconKind.Unsyncable, templateImage("icErrorSmall") }
            };

            UIImage templateImage(string iconName)
                => UIImage.FromBundle(iconName)
                      .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
        }

        public CalendarItemView(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            BackgroundView = new UIView()
            {
                BackgroundColor = UIColor.White
            };

            prepareInitialConstraints();

            ContentView.BringSubviewToFront(TopDragIndicator);
            ContentView.BringSubviewToFront(BottomDragIndicator);

            topDragIndicatorBorderLayer = new CAShapeLayer();
            configureDragIndicatorBorderLayer(TopDragIndicator, topDragIndicatorBorderLayer);
            bottomDragIndicatorBorderLayer = new CAShapeLayer();
            configureDragIndicatorBorderLayer(BottomDragIndicator, bottomDragIndicatorBorderLayer);

            void configureDragIndicatorBorderLayer(UIView dragIndicator, CAShapeLayer borderLayer)
            {
                var rect = dragIndicator.Bounds;
                borderLayer.Path = UIBezierPath.FromOval(rect).CGPath;
                borderLayer.BorderWidth = 2;
                borderLayer.FillColor = UIColor.Clear.CGColor;
                dragIndicator.Layer.AddSublayer(borderLayer);
            }
        }

        protected override void UpdateView()
        {
            var color = itemColor();
            DescriptionLabel.Text = Item.Description;
            DescriptionLabel.TextColor = textColor(color);
            ContentView.BackgroundColor = backgroundColor(Item.Source, color);
            updateIcon(color);
            updateConstraints();
            updateDragIndicators(color);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            updateShadow();
            updateConstraints();
        }

        private UIColor itemColor()
            => MvxColor.ParseHexString(Item.Color).ToNativeColor();

        private UIColor backgroundColor(CalendarItemSource source, UIColor color)
        {
            switch (source)
            {
                case CalendarItemSource.Calendar:
                    return color.ColorWithAlpha((nfloat)0.24);
                case CalendarItemSource.TimeEntry:
                    return color;
                default:
                    throw new ArgumentException("Unexpected calendar item source");
            }
        }

        private UIColor textColor(UIColor color)
        {
            switch (Item.Source)
            {
                case CalendarItemSource.Calendar:
                    return color;
                case CalendarItemSource.TimeEntry:
                    return Item.ForegroundColor().ToNativeColor();
                default:
                    throw new ArgumentException("Unexpected calendar item source");
            }
        }

        private void updateIcon(UIColor color)
        {
            if (Item.IconKind == CalendarIconKind.None)
            {
                CalendarIconImageView.Hidden = true;
                return;
            }

            CalendarIconImageView.Hidden = false;
            CalendarIconImageView.TintColor = textColor(color);
            CalendarIconImageView.Image = images[Item.IconKind];
        }

        private void updateDragIndicators(UIColor color)
        {
            TopDragIndicator.Hidden = !IsEditing;
            BottomDragIndicator.Hidden = !IsEditing;
            topDragIndicatorBorderLayer.StrokeColor = color.CGColor;
            bottomDragIndicatorBorderLayer.StrokeColor = color.CGColor;
        }

        private void prepareInitialConstraints()
        {
            // The following constraints are required because there's an undocumented change in iOS 12
            ContentView.TranslatesAutoresizingMaskIntoConstraints = false;
            ContentView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            ContentView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            ContentView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            ContentView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
        }

        private void updateConstraints()
        {
            CalendarIconWidthConstrarint.Constant
                = CalendarIconHeightConstrarint.Constant
                = iconSize();

            CalendarIconBaselineConstraint.Active = !shouldCenterIconVertically;
            CalendarIconCenterVerticallyConstraint.Active = shouldCenterIconVertically;

            DescriptionLabelLeadingConstraint.Constant = descriptionLabelLeadingConstraintConstant();
            DescriptionLabelTopConstraint.Constant
                = DescriptionLabelBottomConstraint.Constant
                = descriptionLabelTopAndBottomConstraintConstant();

        }

        private int descriptionLabelLeadingConstraintConstant()
        {
            if (Item.IconKind == CalendarIconKind.None)
                return 5;

            return shortCalendarItem ? 18 : 24;
        }

        private int iconSize()
            => shortCalendarItem ? 8 : 14;

        private int descriptionLabelTopAndBottomConstraintConstant()
            => shortCalendarItem ? 0 : 6;

        private void updateShadow()
        {
            if (isEditing)
            {
                var shadowPath = UIBezierPath.FromRect(Bounds);
                Layer.ShadowPath?.Dispose();
                Layer.ShadowPath = shadowPath.CGPath;

                Layer.CornerRadius = 2;
                Layer.ShadowRadius = 4;
                Layer.ShadowOpacity = 0.1f;
                Layer.MasksToBounds = false;
                Layer.ShadowOffset = new CGSize(0, 4);
                Layer.ShadowColor = UIColor.Black.CGColor;
            }
            else
            {
                Layer.ShadowOpacity = 0;
            }
        }
    }
}
