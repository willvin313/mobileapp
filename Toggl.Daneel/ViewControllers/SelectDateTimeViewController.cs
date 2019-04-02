using System;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public partial class SelectDateTimeViewController : ReactiveViewController<SelectDateTimeViewModel>
    {
        public SelectDateTimeViewController() 
            : base(nameof(SelectDateTimeViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Startdate;
            SaveButton.SetTitle(Resources.Save, UIControlState.Normal);

            prepareDatePicker();

            DatePicker.MinimumDate = ViewModel.MinDate.ToNSDate();
            DatePicker.MaximumDate = ViewModel.MaxDate.ToNSDate();

            DatePicker.Rx().Date()
                .Subscribe(ViewModel.CurrentDateTime.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentDateTime
                .Subscribe(DatePicker.Rx().DateTimeObserver())
                .DisposedBy(DisposeBag);

            SaveButton.Rx()
                .BindAction(ViewModel.SaveCommand)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.CloseCommand)
                .DisposedBy(DisposeBag);
        }

        private void prepareDatePicker()
        {
            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                //ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = View.Frame.Height
            };

            DatePicker.Locale = NSLocale.CurrentLocale;
            DatePicker.Mode = UIDatePickerMode.Date;
        }
    }
}
