using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Daneel.Extensions.Reactive;
using UIKit;
using System;
using System.Reactive.Linq;
using static Toggl.Multivac.Extensions.ReactiveExtensions;
using Toggl.Daneel.Views.StartTimeEntry;

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

            TitleLabel.Text = Resources.Projects;
            TextField.Placeholder = Resources.AddFilterProjects;
            EmptyStateLabel.Text = Resources.EmptyProjectText;
            
            var source = new SelectProjectTableViewSource(ProjectsTableView);
            ProjectsTableView.Source = source;
            source.ToggleTasksCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);

            var bindingSet = this.CreateBindingSet<SelectProjectViewController, SelectProjectViewModel>();

            bindingSet.Bind(EmptyStateLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsEmpty);

            bindingSet.Bind(EmptyStateImage)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsEmpty);

            //Table view
            bindingSet.Bind(source)
                      .For(v => v.ObservableCollection)
                      .To(vm => vm.Suggestions);

            bindingSet.Bind(source)
                      .For(v => v.CreateCommand)
                      .To(vm => vm.CreateProjectCommand);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);

            bindingSet.Bind(source)
                      .For(v => v.UseGrouping)
                      .To(vm => vm.UseGrouping);

            bindingSet.Bind(source)
                      .For(v => v.Text)
                      .To(vm => vm.Text);
            
            //Text
            bindingSet.Bind(TextField).To(vm => vm.Text);

            bindingSet.Bind(TextField)
                      .For(v => v.BindPlaceholder())
                      .To(vm => vm.PlaceholderText);

            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(source)
                      .For(s => s.SelectionChangedCommand)
                      .To(vm => vm.SelectProjectCommand);
            
            bindingSet.Apply();

            TextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.Close.Execute();
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
