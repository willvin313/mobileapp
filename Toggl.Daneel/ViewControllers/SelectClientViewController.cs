using System.Threading.Tasks;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectClientViewController : KeyboardAwareViewController<SelectClientViewModel>, IDismissableViewController
    {
        public SelectClientViewController()
            : base(nameof(SelectClientViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new ClientTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            var bindingSet = this.CreateBindingSet<SelectClientViewController, SelectClientViewModel>();

            bindingSet.Bind(source).To(vm => vm.Suggestions);
            bindingSet.Bind(SearchTextField).To(vm => vm.Text);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectClientCommand);

            bindingSet.Bind(source)
                      .For(v => v.CreateClientCommand)
                      .To(vm => vm.CreateClientCommand);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);

            bindingSet.Bind(source)
                      .For(v => v.Text)
                      .To(vm => vm.Text);

            bindingSet.Apply();

            SearchTextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.CloseCommand.ExecuteAsync();
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
