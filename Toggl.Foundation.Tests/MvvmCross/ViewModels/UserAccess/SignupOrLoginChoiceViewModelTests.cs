using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.UserAccess;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.UserAccess
{
    public sealed class SignupOrLoginChoiceViewModelTests
    {
        public abstract class SignupOrLoginChoiceViewModelTest : BaseViewModelTests<SignupOrLoginChoiceViewModel>
        {
            protected override SignupOrLoginChoiceViewModel CreateViewModel() =>
                new SignupOrLoginChoiceViewModel(NavigationService);
        }

        public sealed class TheConstructor : SignupOrLoginChoiceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new AboutViewModel(null);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartLoginFlowAction : SignupOrLoginChoiceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginFlow()
            {
                await ViewModel.StartLoginFlow.Execute();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }
        }

        public sealed class TheStartSignUpFlowAction : SignupOrLoginChoiceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSignUpFlow()
            {
                await ViewModel.StartSignUpFlow.Execute();

                await NavigationService.Received().Navigate<SignupViewModel>();
            }
        }
    }
}
