using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Multivac.Extensions;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class EditProjectViewController : ReactiveViewController<EditProjectViewModel>, IDismissableViewController
    {
        private const float nameAlreadyTakenHeight = 16;

        public EditProjectViewController() 
            : base(nameof(EditProjectViewController))
        {
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.Close.Execute();
            return true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NameTakenErrorLabel.Text = Resources.NameTakenError;
            TitleLabel.Text = Resources.NewProject;
            NameTextField.Placeholder = Resources.ProjectName;
            DoneButton.SetTitle(Resources.Create, UIControlState.Normal);

            //Commands
            DoneButton.Rx().BindAction(ViewModel.Save);
            CloseButton.Rx().BindAction(ViewModel.Close);
            ClientLabel.Rx().BindAction(ViewModel.PickClient);
            WorkspaceLabel.Rx().BindAction(ViewModel.PickWorkspace);
            ColorPickerOpeningView.Rx().BindAction(ViewModel.PickColor);
            PrivateProjectSwitch.Rx().BindAction(ViewModel.TogglePrivateProject);

            //State
            ViewModel.NameIsAlreadyTaken
                .Select<bool, nfloat>(nameIsTaken => nameIsTaken ? nameAlreadyTakenHeight : 0)
                .Subscribe(ProjectNameUsedErrorTextHeight.Rx().Constant())
                .DisposedBy(DisposeBag);

            ViewModel.Name
                .Subscribe(NameTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(WorkspaceLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.Color
                .Select(color => color.ToNativeColor())
                .Subscribe(ColorCircleView.Rx().BackgroundColor())
                .DisposedBy(DisposeBag);

            var emptyString = Resources.AddClient.PrependWithAddIcon(ClientLabel.Font.CapHeight);

            ViewModel.ClientName
                .Select(addNewText)
                .Subscribe(ClientLabel.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            NSAttributedString addNewText(string clientName)
                => string.IsNullOrEmpty(clientName) ? emptyString : new NSAttributedString(clientName);
        }
    }
}

