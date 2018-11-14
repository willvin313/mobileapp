using System;
using System.Threading.Tasks;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectWorkspaceViewController : KeyboardAwareViewController<SelectWorkspaceViewModel>, IDismissableViewController
    {
        public SelectWorkspaceViewController()
            : base(nameof(SelectWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new WorkspaceTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            var bindingSet = this.CreateBindingSet<SelectWorkspaceViewController, SelectWorkspaceViewModel>();

            bindingSet.Bind(TitleLabel).To(vm => vm.Title);

            bindingSet.Bind(source).To(vm => vm.Suggestions);
            bindingSet.Bind(SearchTextField).To(vm => vm.Text);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectWorkspaceCommand);

            bindingSet.Bind(SuggestionsTableViewConstraint)
                      .For(v => v.Constant)
                      .To(vm => vm.AllowQuerying)
                      .WithConversion(new BoolToConstantValueConverter<nfloat>(72, 24));

            bindingSet.Apply();

            if (ViewModel.AllowQuerying)
                SearchTextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.CloseCommand.ExecuteAsync();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            var bottomPosition = UIScreen.MainScreen.Bounds.Height - View.Frame.Y - View.Frame.Height;
            var constant = e.FrameEnd.Height - bottomPosition;
            BottomConstraint.AnimateSetConstant(constant, View);
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
            => BottomConstraint.AnimateSetConstant(0, View);
    }
}

