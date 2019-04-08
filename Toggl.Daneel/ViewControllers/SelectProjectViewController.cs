﻿using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Daneel.Extensions.Reactive;
using UIKit;
using System;
using System.Reactive.Linq;
using static Toggl.Multivac.Extensions.ReactiveExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectProjectViewController : KeyboardAwareViewController<SelectProjectViewModel>, IDismissableViewController
    {
        public SelectProjectViewController() 
            : base(nameof(SelectProjectViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new SelectProjectTableViewSource();
            source.RegisterViewCells(ProjectsTableView);

            source.UseGrouping = ViewModel.UseGrouping;

            ProjectsTableView.TableFooterView = new UIView();
            ProjectsTableView.Source = source;

            ViewModel.Suggestions
                .Subscribe(ProjectsTableView.Rx().ReloadSections(source))
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateImage.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.PlaceholderText
                .Subscribe(TextField.Rx().PlaceholderText())
                .DisposedBy(DisposeBag);

            TextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectProject.Inputs)
                .DisposedBy(DisposeBag);

            source.ToggleTaskSuggestions
                .Subscribe(ViewModel.ToggleTaskSuggestions.Inputs)
                .DisposedBy(DisposeBag);

            TextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }
    }
}
