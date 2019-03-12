using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;
using TextFieldInfo = Toggl.Foundation.Autocomplete.TextFieldInfo;
using CollectionSections = System.Collections.Generic.IEnumerable<Toggl.Foundation.MvvmCross.Collections.SectionModel<string, Toggl.Foundation.Autocomplete.Suggestions.AutocompleteSuggestion>>;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class StartTimeEntryViewModelTests
    {
        public abstract class StartTimeEntryViewModelTest : BaseViewModelTests<StartTimeEntryViewModel>
        {
            protected const string TagName = "Mobile";

            protected const long TagId = 20;
            protected const long TaskId = 30;
            protected const long ProjectId = 10;
            protected const long WorkspaceId = 40;
            protected const string WorkspaceName = "The best workspace ever";
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";

            protected ITestableObserver<TextFieldInfo> TextFieldInfoObserver { get; private set; }

            protected StartTimeEntryParameters DefaultParameter { get; } =
                new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null, null);

            protected override void AdditionalSetup()
            {
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()
                ).Returns(Observable.Return(true));
            }

            protected override void AdditionalViewModelSetup()
            {
                TextFieldInfoObserver = TestScheduler.CreateObserver<TextFieldInfo>();
                ViewModel.TextFieldInfo.Subscribe(TextFieldInfoObserver);
            }

            protected override StartTimeEntryViewModel CreateViewModel()
                => new StartTimeEntryViewModel(
                    TimeService,
                    DataSource,
                    DialogService,
                    UserPreferences,
                    OnboardingStorage,
                    InteractorFactory,
                    NavigationService,
                    AnalyticsService,
                    AutocompleteProvider,
                    SchedulerProvider,
                    IntentDonationService,
                    StopwatchProvider,
                    RxActionFactory
                );
        }

        public sealed class TheConstructor : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useDialogService,
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useNavigationService,
                bool useAnalyticsService,
                bool useAutocompleteProvider,
                bool useSchedulerProvider,
                bool useIntentDonationService,
                bool useStopwatchProvider,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var autocompleteProvider = useAutocompleteProvider ? AutocompleteProvider : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new StartTimeEntryViewModel(
                        timeService,
                        dataSource,
                        dialogService,
                        userPreferences,
                        onboardingStorage,
                        interactorFactory,
                        navigationService,
                        analyticsService,
                        autocompleteProvider,
                        schedulerProvider,
                        intentDonationService,
                        stopwatchProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : StartTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheDateAccordingToTheDateParameterReceived(string placeholder)
            {
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, placeholder, TimeSpan.Zero, null);
                ViewModel.Prepare(parameter);

                ViewModel.PlaceholderText.Should().Be(placeholder);
            }

            [Fact, LogIfTooSlow]
            public void StarsTheTimerWhenDurationIsNull()
            {
                var observable = Substitute.For<IConnectableObservable<DateTimeOffset>>();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var parameter = StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now);

                ViewModel.Prepare(parameter);

                TimeService.CurrentDateTimeObservable.ReceivedWithAnyArgs().Subscribe(null);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheDisplayedTimeToTheValueOfTheDurationParameter()
            {
                var duration = TimeSpan.FromSeconds(130);
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration, null);
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.DisplayedTime.Subscribe(observer);

                ViewModel.Prepare(parameter);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().Be(duration.ToFormattedString(DurationFormat.Improved));
            }
        }

        public sealed class TheInitializeMethod : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChecksIfBillableIsAvailableForTheDefaultWorkspace(bool billableValue)
            {
                var workspace = new MockWorkspace { Id = 10 };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                InteractorFactory.IsBillableAvailableForWorkspace(workspace.Id).Execute()
                    .Returns(Observable.Return(billableValue));
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsBillableAvailable.Subscribe(observer);
                ViewModel.Suggestions.Subscribe();

                var parameter = new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null, null);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();

                TestScheduler.Start();
                observer.LastEmittedValue().Should().Be(billableValue);
            }
        }

        public abstract class CreateSuggestionCellModels : StartTimeEntryViewModelTest
        {
            protected abstract int MaxLength { get; }
            protected abstract char QuerySymbol { get; }
            protected abstract string QueryWithExactSuggestionMatch { get; }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfSuggestingTimeEntries()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("", 0));

                TestScheduler.Start();
                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfTheCurrentQueryIsEmpty()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}", 1));

                TestScheduler.Start();
                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfTheCurrentQueryIsOnlyWhitespace()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}    ", 1));

                TestScheduler.Start();
                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfTheCurrentQueryIsLongerThanMaxLength()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}{createLongString(MaxLength + 1)}",
                    1));

                TestScheduler.Start();
                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfSuchSuggestionAlreadyExists()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}{QueryWithExactSuggestionMatch}",
                    1));

                TestScheduler.Start();

                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task WontExistIfWorkspaceSettingsDisableProjectCreation()
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                var workspace = new MockWorkspace { Id = 1, Admin = false, OnlyAdminsMayCreateProjects = true };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));

                await ViewModel.Initialize();

                TestScheduler.Start();
                observer.LastEmittedValue()
                    .Where(hasCreateSuggestions)
                    .Should().BeEmpty();
            }

            private string createLongString(int length)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append('A'))
                    .ToString();

            private bool hasCreateSuggestions(SectionModel<string, AutocompleteSuggestion> collection)
            {
                return collection.Items.Any(suggestion => suggestion is CreateEntitySuggestion);

            }

            public sealed class WhenSuggestingProjects : CreateSuggestionCellModels
            {
                protected override int MaxLength => MaxProjectNameLengthInBytes;
                protected override char QuerySymbol => '@';
                protected override string QueryWithExactSuggestionMatch => ProjectName;

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(10);
                    project.Name.Returns(ProjectName);
                    project.WorkspaceId.Returns(40);
                    project.Workspace.Name.Returns("Some workspace");
                    var projectSuggestion = new ProjectSuggestion(project);

                    AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Projects))
                        .Returns(Observable.Return(new ProjectSuggestion[] { projectSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                    ViewModel.Suggestions.Subscribe(); // Otherwise the observable chaing won't trigger
                }

                [Fact, LogIfTooSlow]
                public async Task WontExistIfAProjectIsAlreadySelected()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde @fgh", 10);

                    ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().BeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task WontExistIfAProjectIsAlreadySelectedEvenIfInProjectSelectionMode()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde @fgh", 10);

                    ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.ToggleProjectSuggestions.Execute();

                    ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().BeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task ExistIfTheProjectNameIsValid()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    var workspace = new MockWorkspace { Id = 1, Name = "ws", Admin = true };
                    InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new[] { workspace }));

                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    TestScheduler.Start();

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("@bongo", 6));

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().NotBeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task ExistEvenIfAProjectWithSameNameExist()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    var workspace = new MockWorkspace { Id = 1, Name = "ws", Admin = true };
                    InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new[] { workspace }));

                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    TestScheduler.Start();

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"@${ProjectName}", 6));

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().NotBeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksProjectSelection()
                {
                    ViewModel.Prepare();

                    await ViewModel.Initialize();

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("abcde @fgh", 10));

                    TestScheduler.Start();
                    AnalyticsService.StartEntrySelectProject.Received().Track(ProjectTagSuggestionSource.TextField);
                }
            }

            public sealed class WhenSuggestingTags : CreateSuggestionCellModels
            {
                protected override int MaxLength => MaxTagNameLengthInBytes;
                protected override char QuerySymbol => '#';
                protected override string QueryWithExactSuggestionMatch => TagName;

                public WhenSuggestingTags()
                {
                    var tag = Substitute.For<IThreadSafeTag>();
                    tag.Id.Returns(20);
                    tag.Name.Returns(TagName);
                    var tagSuggestion = new TagSuggestion(tag);

                    AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Tags))
                        .Returns(Observable.Return(new TagSuggestion[] { tagSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                    ViewModel.Suggestions.Subscribe(); // Otherwise the observable chaing won't trigger

                }

                [Fact, LogIfTooSlow]
                public async Task ExistNoMatterThatAProjectIsAlreadySelected()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde #fgh", 10);

                    await ViewModel.Initialize();

                    ViewModel.SetTextSpans.ExecuteSequentally(
                        new List<ISpan> { projectSpan },
                        new List<ISpan> { projectSpan, querySpan }
                        )
                        .Subscribe();

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().NotBeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task ExistNoMatterThatAProjectIsAlreadySelectedAndInTagSuccestionMode()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde #fgh", 10);

                    await ViewModel.Initialize();
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    ViewModel.SetTextSpans.ExecuteWithCompletion(new List<ISpan> { projectSpan })
                        .PrependAction(ViewModel.ToggleTagSuggestions)
                        .PrependAction(ViewModel.SetTextSpans, new List<ISpan> { projectSpan, querySpan })
                        .Subscribe();

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().NotBeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task WontExistIfAnOtherTagIsAvailableWithSameNameButDifferentCase()
                {
                    var observer = TestScheduler.CreateObserver<CollectionSections>();
                    ViewModel.Suggestions.Subscribe(observer);

                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("#mobile", 7);

                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.ToggleTagSuggestions.Execute();

                    ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);
                    TestScheduler.Start();

                    TestScheduler.Start();
                    observer.LastEmittedValue()
                        .Where(hasCreateSuggestions)
                        .Should().BeEmpty();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksTagSelection()
                {
                    ViewModel.Prepare();
                    TestScheduler.Start();

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("abcde #fgh", 10));

                    AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.TextField);
                }
            }
        }

        public sealed class TheCreateProjectSuggestion
        {
            public sealed class WhenSuggestingProjects : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(10);
                    InteractorFactory.GetProjectById(Arg.Any<long>()).Execute().Returns(Observable.Return(project));
                    ViewModel.OnTextFieldInfoFromView(
                        new QueryTextSpan($"@{currentQuery}", 15)
                    );

                    ViewModel.Prepare();
                    ViewModel.Prepare(DefaultParameter);

                    TestScheduler.Start();
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateProjectViewModel()
                {
                    ViewModel.Suggestions.Subscribe();

                    ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateProject, ""));

                    TestScheduler.Start();
                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public async Task UsesTheCurrentQueryAsTheParameterForTheCreateProjectViewModel()
                {
                    ViewModel.Suggestions.Subscribe();

                    ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateProject, ""));

                    TestScheduler.Start();
                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(currentQuery);
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedProject()
                {
                    long projectId = 200;
                    NavigationService
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Is(currentQuery))
                        .Returns(projectId);
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(currentQuery);
                    InteractorFactory.GetProjectById(Arg.Is(projectId)).Execute().Returns(Observable.Return(project));
                    ViewModel.Suggestions.Subscribe();

                    ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateProject, ""));

                    TestScheduler.Start();
                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.ProjectName.Should().Be(currentQuery);
                }
            }
        }

        public sealed class WhenSuggestingTags : StartTimeEntryViewModelTest
        {
            private const string currentQuery = "My awesome Toggl project";

            private readonly QueryTextSpan querySpan = new QueryTextSpan($"#{currentQuery}", 1);

            public WhenSuggestingTags()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                TestScheduler.Start();
                ViewModel.OnTextFieldInfoFromView(querySpan);
                TestScheduler.Start();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesTagWithCurrentQueryAsName()
            {
                ViewModel.Suggestions.Subscribe();

                ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateTag, ""));

                TestScheduler.Start();
                await InteractorFactory
                    .Received()
                    .CreateTag(Arg.Is(currentQuery), Arg.Any<long>())
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesTagInProjectsWorkspaceIfAProjectIsSelected()
            {
                ViewModel.Suggestions.Subscribe();

                long workspaceId = 100;
                long projectId = 101;
                var project = new MockProject
                {
                    Id = projectId,
                    WorkspaceId = workspaceId
                };
                var projectSpan = new ProjectSpan(projectId, "Project", "0000AF", null, null);
                var tagSuggestion = new CreateEntitySuggestion(Resources.CreateTag, "");

                InteractorFactory.GetProjectById(Arg.Is(projectId)).Execute().Returns(Observable.Return(project));

                ViewModel.SelectSuggestion.ExecuteWithCompletion(new ProjectSuggestion(project))
                    .PrependAction(ViewModel.SetTextSpans, new List<ISpan> { querySpan, projectSpan })
                    .PrependAction(ViewModel.SelectSuggestion, tagSuggestion)
                    .Subscribe();

                TestScheduler.Start();
                InteractorFactory
                    .Received()
                    .CreateTag(Arg.Any<string>(), Arg.Is(workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesTagInUsersDefaultWorkspaceIfNoProjectIsSelected()
            {
                long workspaceId = 100;
                var workspace = new MockWorkspace { Id = workspaceId };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                var user = Substitute.For<IThreadSafeUser>();
                user.DefaultWorkspaceId.Returns(workspaceId);
                DataSource.User.Get().Returns(Observable.Return(user));
                await ViewModel.Initialize();

                ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateTag, ""));

                TestScheduler.Start();
                await InteractorFactory
                    .Received()
                    .CreateTag(Arg.Any<string>(), Arg.Is(workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SelectsTheCreatedTag()
            {
                var tagId = 0;
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(tagId);
                tag.Name.Returns(currentQuery);

                InteractorFactory
                    .CreateTag(Arg.Any<string>(), Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(tag));

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe();

                ViewModel.SelectSuggestion.Execute(new CreateEntitySuggestion(Resources.CreateTag, ""));

                TestScheduler.Start();
                var tags = TextFieldInfoObserver.LastEmittedValue().GetTagSpan();
                tags.TagName.Should().Be(currentQuery);
            }
        }

        public sealed class TheBackAction : StartTimeEntryViewModelTest
        {
            public TheBackAction()
            {
                var parameter = StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now);
                ViewModel.Prepare(parameter);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelIfUserDoesNotChangeAnything()
            {
                ViewModel.Close.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsAConfirmationDialogIfUserEnteredSomething()
            {
                makeDirty();

                ViewModel.Close.Execute();

                TestScheduler.Start();
                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewIfUserWantsToContinueEditing()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                    .Returns(_ => Observable.Return(false));

                ViewModel.Close.Execute();

                TestScheduler.Start();
                await NavigationService.DidNotReceive().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewIfUserWantsToDiscardTheEnteredInformation()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                    .Returns(_ => Observable.Return(true));

                ViewModel.Close.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallTheAnalyticsServiceSinceNoTimeEntryWasCreated()
            {
                ViewModel.Close.Execute();

                TestScheduler.Start();
                AnalyticsService.DidNotReceive().Track(Arg.Any<StartTimeEntryEvent>());
            }

            private void makeDirty()
            {
                ViewModel.ToggleBillable.Execute();
            }
        }

        public sealed class TheToggleBillableAction : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TogglesTheIsBillableProperty()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsBillable.Subscribe(observer);

                ViewModel.ToggleBillable.Execute();
                TestScheduler.Start();

                observer.Values().Count().Should().Be(2);
                var billableValues = observer.Values().ToList();
                billableValues[0].Should().Be(!billableValues[1]);
            }

            [Fact, LogIfTooSlow]
            public void TracksBillableTap()
            {
                ViewModel.ToggleBillable.Execute();

                AnalyticsService.Received()
                                .StartViewTapped
                                .Track(StartViewTapSource.Billable);
            }
        }

        public sealed class TheToggleProjectSuggestionsAction : StartTimeEntryViewModelTest
        {
            public TheToggleProjectSuggestionsAction()
            {
                var suggestions = ProjectSuggestion.FromProjects(Enumerable.Empty<IThreadSafeProject>());
                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(info => info.Text.Contains("@")))
                    .Returns(Observable.Return(suggestions));

                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(suggestions));

                var defaultWorkspace = new MockWorkspace { Id = WorkspaceId };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(defaultWorkspace));
            }

            private List<IThreadSafeProject> createProjects(int count)
                => Enumerable
                    .Range(0, count)
                    .Select(i =>
                    {
                        var workspace = Substitute.For<IThreadSafeWorkspace>();
                        workspace.Id.Returns(WorkspaceId);
                        workspace.Name.Returns(WorkspaceName);

                        var project = Substitute.For<IThreadSafeProject>();
                        project.Workspace.Returns(workspace);
                        project.WorkspaceId.Returns(WorkspaceId);
                        project.Id.Returns(ProjectId + i);
                        project.Name.Returns($"{ProjectName}-{i}");
                        project.Color.Returns(ProjectColor);
                        return project;
                    })
                    .ToList();

            [Fact, LogIfTooSlow]
            public async Task StartProjectSuggestionEvenIfTheProjectHasAlreadyBeenSelected()
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan(Description, Description.Length),
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null)
                );

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsSuggestingProjects.Subscribe(observer);

                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                observer.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingProjectsPropertyToTrueIfNotInProjectSuggestionMode()
            {
                var isSuggestingObserver = TestScheduler.CreateObserver<bool>();
                var suggestionObserver = TestScheduler.CreateObserver<IList<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.Suggestions.Subscribe(suggestionObserver);
                ViewModel.IsSuggestingProjects.Subscribe(isSuggestingObserver);

                ViewModel.ToggleProjectSuggestions.Execute();
                TestScheduler.Start();

                isSuggestingObserver.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsCorrectProjectSuggestionsAfterProjectHasAlreadyBeenSelected()
            {
                var projects = createProjects(5);
                var noProjectCount = 1;
                var projectSuggestions = ProjectSuggestion.FromProjects(projects);
                var chosenProject = projectSuggestions.First();
                DataSource.Projects.GetAll().Returns(Observable.Return(projects));
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(Observable.Return(projectSuggestions));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(Description, Description.Length));
                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                ViewModel.ToggleProjectSuggestions.ExecuteWithCompletion()
                    .PrependAction(ViewModel.SelectSuggestion, chosenProject)
                    .PrependAction(ViewModel.ToggleProjectSuggestions)
                    .Subscribe();

                TestScheduler.Start();
                observer.LastEmittedValue().Should().HaveCount(1);
                observer.LastEmittedValue().First().Items.Should().HaveCount(projects.Count + noProjectCount);
            }

            [Fact, LogIfTooSlow]
            public async Task ProjectSuggestionsAreClearedOnProjectSelection()
            {
                var projects = createProjects(5);
                var projectSuggestions = ProjectSuggestion.FromProjects(projects);
                var chosenProject = projectSuggestions.First();
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(Observable.Return(projectSuggestions));

                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(Description, Description.Length));

                var suggestionsObserver = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(suggestionsObserver);
                var isSuggestingProjectsObserver = TestScheduler.CreateObserver<bool>();
                ViewModel.IsSuggestingProjects.Subscribe(isSuggestingProjectsObserver);

                ViewModel.ToggleProjectSuggestions.Execute();
                ViewModel.SelectSuggestion.Execute(chosenProject);

                TestScheduler.Start();
                isSuggestingProjectsObserver.LastEmittedValue().Should().BeFalse();
                suggestionsObserver.LastEmittedValue().Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task AddsAnAtSymbolAtTheEndOfTheQueryInOrderToStartProjectSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} @";
                ViewModel.Prepare(DefaultParameter);
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                var querySpan = TextFieldInfoObserver.LastEmittedValue().GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("@")]
            [InlineData("@somequery")]
            [InlineData("@some query")]
            [InlineData("@some query@query")]
            [InlineData("Testing Toggl Apps @")]
            [InlineData("Testing Toggl Apps @somequery")]
            [InlineData("Testing Toggl Apps @some query")]
            [InlineData("Testing Toggl Apps @some query @query")]
            public void SetsTheIsSuggestingProjectsPropertyToFalseIfAlreadyInProjectSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));
                var isSuggestingProjectsObserver = TestScheduler.CreateObserver<bool>();
                ViewModel.IsSuggestingProjects.Subscribe(isSuggestingProjectsObserver);

                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                isSuggestingProjectsObserver.LastEmittedValue().Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("@", "")]
            [InlineData("@somequery", "")]
            [InlineData("@some query", "")]
            [InlineData("@some query@query", "")]
            [InlineData("Testing Toggl Apps @", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query @query", "Testing Toggl Apps ")]
            public void RemovesTheAtSymbolFromTheDescriptionTextIfAlreadyInProjectSuggestionMode(
                string description, string expected)
            {
                ViewModel.Suggestions.Subscribe();

                ViewModel.Prepare(DefaultParameter);
                ViewModel.SetTextSpans.Execute( new List<ISpan> { new QueryTextSpan(description, description.Length) });

                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                var querySpan = TextFieldInfoObserver.LastEmittedValue().GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }

            [Fact, LogIfTooSlow]
            public void TracksShowProjectSuggestions()
            {
                ViewModel.ToggleProjectSuggestions.Execute();

                TestScheduler.Start();
                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Project);
                AnalyticsService.StartEntrySelectProject.Received().Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            }
        }

        public sealed class TheToggleTagSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleTagSuggestionsCommand()
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(TagId);
                tag.Name.Returns(TagName);
                var suggestions = TagSuggestion.FromTags(new[] { tag });
                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(info => info.Text.Contains("#")))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingTagsPropertyToTrueIfNotInTagSuggestionMode()
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.Suggestions.Subscribe();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsSuggestingTags.Subscribe(observer);

                ViewModel.ToggleTagSuggestions.Execute();

                TestScheduler.Start();
                observer.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task AddsHashtagSymbolAtTheEndOfTheQueryInOrderToTagSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} #";
                ViewModel.Prepare(DefaultParameter);
                ViewModel.Suggestions.Subscribe();
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleTagSuggestions.Execute();

                TestScheduler.Start();
                var querySpan = TextFieldInfoObserver.LastEmittedValue().GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("#")]
            [InlineData("#somequery")]
            [InlineData("#some query")]
            [InlineData("#some quer#query")]
            [InlineData("Testing Toggl Apps #")]
            [InlineData("Testing Toggl Apps #somequery")]
            [InlineData("Testing Toggl Apps #some query")]
            [InlineData("Testing Toggl Apps #some query #query")]
            public async Task SetsTheIsSuggestingTagsPropertyToFalseIfAlreadyInTagSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.Suggestions.Subscribe();
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsSuggestingTags.Subscribe(observer);

                ViewModel.ToggleTagSuggestions.Execute();

                TestScheduler.Start();
                observer.LastEmittedValue().Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("#", "")]
            [InlineData("#somequery", "")]
            [InlineData("#some query", "")]
            [InlineData("#some query#query", "")]
            [InlineData("Testing Toggl Apps #", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query #query", "Testing Toggl Apps ")]
            public async Task RemovesTheHashtagSymbolFromTheDescriptionTextIfAlreadyInTagSuggestionMode(
                string description, string expected)
            {
                ViewModel.Suggestions.Subscribe();

                var observer = TestScheduler.CreateObserver<TextFieldInfo>();
                ViewModel.TextFieldInfo.Subscribe(observer);

                ViewModel.Prepare(DefaultParameter);
                ViewModel.Suggestions.Subscribe();

                ViewModel.SetTextSpans.ExecuteSequentally(
                    new List<ISpan> { new ProjectSpan(ProjectId, ProjectName, ProjectColor) },
                    new List<ISpan> { new QueryTextSpan(description, description.Length) }
                    )
                    .PrependAction(ViewModel.ToggleTagSuggestions)
                    .Subscribe();

                TestScheduler.Start();

                var querySpan = observer.LastEmittedValue().GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleTagSuggestions.Execute();

                TestScheduler.Start();
                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }

            [Fact, LogIfTooSlow]
            public void TracksShowTagSuggestions()
            {
                ViewModel.ToggleTagSuggestions.Execute();

                TestScheduler.Start();
                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Tags);
                AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            }


        }

        public sealed class TheChangeTimeCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task TracksStartTimeTap()
            {
                var now = DateTimeOffset.UtcNow;
                var parameters = StartTimeEntryParameters.ForTimerMode(now);
                var returnParameter = DurationParameter.WithStartAndDuration(now, TimeSpan.FromMinutes(1));
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(Task.FromResult(returnParameter));
                ViewModel.Prepare(parameters);

                ViewModel.ChangeTime.Execute();

                TestScheduler.Start();
                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.StartTime);
            }
        }

        public sealed class TheSetStartDateCommand : StartTimeEntryViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2018, 02, 13, 23, 59, 12, TimeSpan.FromHours(-1));
            private static readonly StartTimeEntryParameters prepareParameters = StartTimeEntryParameters.ForTimerMode(now);

            public TheSetStartDateCommand()
            {
                TimeService.CurrentDateTime.Returns(now);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectDateTimeViewModel()
            {
                ViewModel.Prepare(prepareParameters);

                ViewModel.SetStartDate.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>());
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForARunningTimeEntry()
            {
                ViewModel.Prepare(prepareParameters);

                ViewModel.SetStartDate.Execute();

                TestScheduler.Start();
                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == now - MaxTimeEntryDuration && param.MaxDate == now));
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForAStoppedTimeEntry()
            {
                var stoppedParametsrs = StartTimeEntryParameters.ForManualMode(now);
                ViewModel.Prepare(stoppedParametsrs);

                ViewModel.SetStartDate.Execute();

                TestScheduler.Start();
                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == EarliestAllowedStartTime && param.MaxDate == LatestAllowedStartTime));
            }

            [Fact, LogIfTooSlow]
            public async Task TracksStartDateTap()
            {
                TimeService.CurrentDateTime.Returns(now);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(now);
                ViewModel.Prepare(prepareParameters);

                ViewModel.SetStartDate.Execute();

                TestScheduler.Start();
                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.StartDate);
            }
        }

        public sealed class TheDoneCommand : StartTimeEntryViewModelTest
        {
            private const long userId = 10;
            private const long projectId = 11;
            private const long defaultWorkspaceId = 12;
            private const long projectWorkspaceId = 13;

            private readonly IThreadSafeUser user;
            private readonly IThreadSafeProject project;

            private readonly DateTimeOffset startDate = DateTimeOffset.UtcNow;

            public TheDoneCommand()
            {
                var defaultWorkspace = new MockWorkspace { Id = defaultWorkspaceId };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(defaultWorkspace));

                user = new MockUser
                {
                    Id = userId,
                    DefaultWorkspaceId = defaultWorkspaceId
                };

                project = new MockProject
                {
                    Id = projectId,
                    WorkspaceId = projectWorkspaceId
                };

                DataSource.User.Current
                    .Returns(Observable.Return(user));

                InteractorFactory
                    .GetProjectById(projectId)
                    .Execute()
                    .Returns(Observable.Return(project));

                var parameter = new StartTimeEntryParameters(startDate, "", null, null);
                ViewModel.Prepare(parameter);
                ViewModel.Initialize().GetAwaiter().GetResult();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheCreateTimeEntryInteractor()
            {
                ViewModel.Done.Execute();

                TestScheduler.Start();
                var te =
                    InteractorFactory.Received().CreateTimeEntry(Arg.Any<ITimeEntryPrototype>(), TimeEntryStartOrigin.Timer);
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheCreateTimeEntryInteractor()
            {
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.CreateTimeEntry(Arg.Any<ITimeEntryPrototype>(), TimeEntryStartOrigin.Timer).Returns(mockedInteractor);

                ViewModel.Done.Execute();

                TestScheduler.Start();
                await mockedInteractor.Received().Execute();
            }

            [Theory, LogIfTooSlow]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            public void ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
            {
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, 0));

                ViewModel.Done.Execute();

                TestScheduler.Start();
                InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                    timeEntry.Description.Length == 0
                ), TimeEntryStartOrigin.Timer);
            }

            [Theory, LogIfTooSlow]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public void TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
            {
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.Done.Execute();
                TestScheduler.Start();

                InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                    timeEntry.Description == trimmed
                ), TimeEntryStartOrigin.Timer);
            }

            [Property]
            public void DoesNotCreateARunningTimeEntryWhenDurationIsNotNull(TimeSpan duration)
            {
                if (duration < TimeSpan.Zero) return;

                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration, null);

                ViewModel.Prepare(parameter);
                ViewModel.Done.Execute();

                TestScheduler.Start();

                InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                    timeEntry.Duration.HasValue
                ), TimeEntryStartOrigin.Manual);
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesARunningTimeEntryWhenDurationIsNull()
            {
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null, null);

                ViewModel.Prepare(parameter);
                ViewModel.Done.Execute();

                TestScheduler.Start();
                InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                    timeEntry.Duration.HasValue == false
                ), TimeEntryStartOrigin.Timer);
            }

            private TagSuggestion tagSuggestionFromInt(int i)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(i);
                tag.Name.Returns(i.ToString());

                return new TagSuggestion(tag);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(1);
                user.DefaultWorkspaceId.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null, null);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();

                ViewModel.Done.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheSelectSuggestionCommand
        {
            public abstract class SelectSuggestionTest<TSuggestion> : StartTimeEntryViewModelTest
                where TSuggestion : AutocompleteSuggestion
            {
                protected IThreadSafeTag Tag { get; }
                protected IThreadSafeTask Task { get; }
                protected IThreadSafeProject Project { get; }
                protected IThreadSafeTimeEntry TimeEntry { get; }
                protected IThreadSafeWorkspace Workspace { get; }

                protected abstract TSuggestion Suggestion { get; }

                protected SelectSuggestionTest()
                {
                    Workspace = Substitute.For<IThreadSafeWorkspace>();
                    Workspace.Id.Returns(WorkspaceId);

                    Project = Substitute.For<IThreadSafeProject>();
                    Project.Id.Returns(ProjectId);
                    Project.Name.Returns(ProjectName);
                    Project.Color.Returns(ProjectColor);
                    Project.Workspace.Returns(Workspace);
                    Project.WorkspaceId.Returns(WorkspaceId);
                    Project.Active.Returns(true);

                    Task = Substitute.For<IThreadSafeTask>();
                    Task.Id.Returns(TaskId);
                    Task.Project.Returns(Project);
                    Task.ProjectId.Returns(ProjectId);
                    Task.WorkspaceId.Returns(WorkspaceId);
                    Task.Name.Returns(TaskId.ToString());

                    TimeEntry = Substitute.For<IThreadSafeTimeEntry>();
                    TimeEntry.Description.Returns(Description);
                    TimeEntry.Project.Returns(Project);

                    Tag = Substitute.For<IThreadSafeTag>();
                    Tag.Id.Returns(TagId);
                    Tag.Name.Returns(TagName);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksProjectSelectionWhenProjectSymbolSelected()
                {
                    var projectSuggestion = QuerySymbolSuggestion.Suggestions
                        .First((s) => s.Symbol == QuerySymbols.ProjectsString);

                    ViewModel.SelectSuggestion.Execute(projectSuggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received()
                        .Track(StartViewTapSource.PickEmptyStateProjectSuggestion);
                    AnalyticsService.StartEntrySelectProject.Received()
                        .Track(ProjectTagSuggestionSource.TableCellButton);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksTagSelectionWhenTagSymbolSelected()
                {
                    var tagSuggestion = QuerySymbolSuggestion.Suggestions
                        .First((s) => s.Symbol == QuerySymbols.TagsString);

                    ViewModel.SelectSuggestion.Execute(tagSuggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickEmptyStateTagSuggestion);
                    AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.TableCellButton);
                }
            }

            public abstract class ProjectSettingSuggestion<TSuggestion> : SelectSuggestionTest<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                [Fact, LogIfTooSlow]
                public void SetsTheProjectIdToTheSuggestedProjectId()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.ProjectId.Should().Be(ProjectId);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectNameToTheSuggestedProjectName()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.ProjectName.Should().Be(ProjectName);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectColorToTheSuggestedProjectColor()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);
                    TestScheduler.Start();

                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.ProjectColor.Should().Be(ProjectColor);
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                public void SetsTheAppropriateBillableValue(bool billableValue)
                {
                    var isBillableObserver = TestScheduler.CreateObserver<bool>();
                    var IsBillableAvailableObserver = TestScheduler.CreateObserver<bool>();
                    ViewModel.Suggestions.Subscribe();
                    ViewModel.IsBillable.Subscribe(isBillableObserver);
                    ViewModel.IsBillableAvailable.Subscribe(IsBillableAvailableObserver);

                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory.IsBillableAvailableForProject(ProjectId).Execute()
                        .Returns(Observable.Return(true));
                    InteractorFactory.ProjectDefaultsToBillable(ProjectId).Execute()
                        .Returns(Observable.Return(billableValue));

                    ViewModel.SelectSuggestion.Execute(Suggestion);
                    TestScheduler.Start();

                    isBillableObserver.LastEmittedValue().Should().Be(billableValue);
                    IsBillableAvailableObserver.LastEmittedValue().Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public async Task DisablesBillableIfTheWorkspaceOfTheSelectedProjectDoesNotAllowIt(bool? billableValue)
                {
                    var isBillableObserver = TestScheduler.CreateObserver<bool>();
                    var IsBillableAvailableObserver = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsBillable.Subscribe(isBillableObserver);
                    ViewModel.IsBillableAvailable.Subscribe(IsBillableAvailableObserver);

                    Project.Billable.Returns(billableValue);
                    InteractorFactory.GetProjectById(ProjectId).Execute().Returns(Observable.Return(Project));
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory
                        .IsBillableAvailableForWorkspace(10)
                        .Execute()
                        .Returns(Observable.Return(false));

                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    isBillableObserver.LastEmittedValue().Should().BeFalse();
                    IsBillableAvailableObserver.LastEmittedValue().Should().BeFalse();
                }
            }

            public abstract class ProjectTaskSuggestion<TSuggestion> : ProjectSettingSuggestion<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                protected ProjectTaskSuggestion()
                {
                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("Something @togg", 15));
                }

                [Fact, LogIfTooSlow]
                public async Task RemovesTheProjectQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var querySpan = TextFieldInfoObserver.LastEmittedValue().FirstTextSpan();
                    querySpan.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public async Task ShowsConfirmDialogIfWorkspaceIsAboutToBeChanged()
                {
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    await DialogService.Received().Confirm(
                        Arg.Is(Resources.DifferentWorkspaceAlertTitle),
                        Arg.Is(Resources.DifferentWorkspaceAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotShowConfirmDialogIfWorkspaceIsNotGoingToChange()
                {
                    var workspace = new MockWorkspace { Id = WorkspaceId };
                    InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(WorkspaceId);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    await DialogService.DidNotReceive().Confirm(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>()
                    );
                }

                [Fact, LogIfTooSlow]
                public void ClearsTagsIfWorkspaceIsChanged()
                {
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    ViewModel.Initialize();
                    var suggestions = Enumerable.Range(100, 10)
                        .Select(i =>
                        {
                            var tag = Substitute.For<IThreadSafeTag>();
                            tag.Id.Returns(i);
                            return new TagSuggestion(tag);
                        });

                    ViewModel.SelectSuggestion.ExecuteSequentally(suggestions)
                        .PrependAction(ViewModel.SelectSuggestion, Suggestion)
                        .Subscribe();

                    TestScheduler.Start();
                    var tags = TextFieldInfoObserver.LastEmittedValue().Spans.OfType<TagSpan>();
                    tags.Should().BeEmpty();
                }
            }

            public sealed class WhenSelectingATimeEntrySuggestion : ProjectSettingSuggestion<TimeEntrySuggestion>
            {
                protected override TimeEntrySuggestion Suggestion { get; }

                public WhenSelectingATimeEntrySuggestion()
                {
                    Suggestion = new TimeEntrySuggestion(TimeEntry);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTextFieldInfoTextToTheValueOfTheSuggestedDescription()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var querySpan = TextFieldInfoObserver.LastEmittedValue().FirstTextSpan();
                    querySpan.Text.Should().Be(Description);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTimeEntrySuggestionSelected()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTimeEntrySuggestion);
                }

                [Fact, LogIfTooSlow]
                public async Task ChangesTheWorkspaceIfNeeded()
                {
                    const long expectedWorkspaceId = WorkspaceId + 1;
                    TimeEntry.WorkspaceId.Returns(expectedWorkspaceId);
                    var newSuggestion = new TimeEntrySuggestion(TimeEntry);

                    ViewModel.SelectSuggestion.Execute(newSuggestion);

                    TestScheduler.Start();
                    TextFieldInfoObserver.LastEmittedValue().WorkspaceId.Should().Be(expectedWorkspaceId);
                }
            }

            public sealed class WhenSelectingATaskSuggestion : ProjectTaskSuggestion<TaskSuggestion>
            {
                protected override TaskSuggestion Suggestion { get; }

                public WhenSelectingATaskSuggestion()
                {
                    Suggestion = new TaskSuggestion(Task);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTaskIdToTheSameIdAsTheSelectedSuggestion()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.TaskId.Should().Be(TaskId);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTaskSuggestionSelected()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTaskSuggestion);
                }
            }

            public sealed class WhenSelectingAProjectSuggestion : ProjectTaskSuggestion<ProjectSuggestion>
            {
                protected override ProjectSuggestion Suggestion { get; }

                public WhenSelectingAProjectSuggestion()
                {
                    Suggestion = new ProjectSuggestion(Project);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTaskIdToNull()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var projectSpan = TextFieldInfoObserver.LastEmittedValue().GetProjectSpan();
                    projectSpan.TaskId.Should().BeNull();
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                public void SetsTheAppropriateBillableValueBasedOnTheWorkspaceWhenSelectingNoProject(bool isBillableAvailable)
                {
                    var noProjectSuggestion = ProjectSuggestion.NoProject(WorkspaceId, Workspace.Name);
                    var isBillableObserver = TestScheduler.CreateObserver<bool>();
                    var IsBillableAvailableObserver = TestScheduler.CreateObserver<bool>();
                    ViewModel.Suggestions.Subscribe();
                    ViewModel.IsBillable.Subscribe(isBillableObserver);
                    ViewModel.IsBillableAvailable.Subscribe(IsBillableAvailableObserver);
                    InteractorFactory
                        .IsBillableAvailableForWorkspace(WorkspaceId)
                        .Execute()
                        .Returns(Observable.Return(isBillableAvailable));

                    ViewModel.SelectSuggestion.Execute(noProjectSuggestion);
                    TestScheduler.Start();

                    isBillableObserver.LastEmittedValue().Should().BeFalse();
                    IsBillableAvailableObserver.LastEmittedValue().Should().Be(isBillableAvailable);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenProjectSuggestionSelected()
                {
                    DialogService
                        .Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                        .Returns(Observable.Return(false));

                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickProjectSuggestion);
                }
            }

            public sealed class WhenSelectingATagSuggestion : SelectSuggestionTest<TagSuggestion>
            {
                protected override TagSuggestion Suggestion { get; }

                public WhenSelectingATagSuggestion()
                {
                    Suggestion = new TagSuggestion(Tag);

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("Something #togg", 15));
                }

                [Fact, LogIfTooSlow]
                public async Task RemovesTheTagQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var querySpan = TextFieldInfoObserver.LastEmittedValue().Spans.OfType<TextSpan>().First();
                    querySpan.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public async Task AddsTheSuggestedTagToTheList()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var tags = TextFieldInfoObserver.LastEmittedValue().Spans.OfType<TagSpan>();
                    tags.Should().Contain(t => t.TagId == Suggestion.TagId);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTagSuggestionSelected()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTagSuggestion);
                }
            }

            public sealed class WhenSelectingAQuerySymbolSuggestion : SelectSuggestionTest<QuerySymbolSuggestion>
            {
                protected override QuerySymbolSuggestion Suggestion { get; } = QuerySymbolSuggestion.Suggestions.First();

                [Fact, LogIfTooSlow]
                public async Task SetsTheTextToTheQuerySymbolSelected()
                {
                    ViewModel.SelectSuggestion.Execute(Suggestion);

                    TestScheduler.Start();
                    var querySpan = TextFieldInfoObserver.LastEmittedValue().FirstTextSpan();
                    querySpan.Text.Should().Be(Suggestion.Symbol);
                }
            }
        }

        public sealed class TheDurationTappedCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TracksDurationTap()
            {
                ViewModel.DurationTapped.Execute();

                TestScheduler.Start();
                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Duration);
            }
        }

        public sealed class TheSuggestionsProperty : StartTimeEntryViewModelTest
        {
            private IEnumerable<ProjectSuggestion> getProjectSuggestions(int count, int workspaceId)
            {
                for (int i = 0; i < count; i++)
                    yield return getProjectSuggestion(i, workspaceId);
            }

            private ProjectSuggestion getProjectSuggestion(int projectId, int workspaceId)
            {
                return getProjectSuggestion(projectId, workspaceId, new List<IThreadSafeTask>());
            }

            private ProjectSuggestion getProjectSuggestion(int projectId, int workspaceId, IEnumerable<IThreadSafeTask> tasks)
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Name.Returns($"Workspace{workspaceId}");
                workspace.Id.Returns(workspaceId);
                var project = Substitute.For<IThreadSafeProject>();
                project.WorkspaceId.Returns(workspaceId);
                project.Name.Returns($"Project{projectId}");
                project.Workspace.Returns(workspace);
                project.Active.Returns(true);
                project.Tasks.Returns(tasks);
                return new ProjectSuggestion(project);
            }

            private IEnumerable<string> projectNames(IEnumerable<AutocompleteSuggestion> autocompleteSuggestions)
                => autocompleteSuggestions.Cast<ProjectSuggestion>().Select(suggestion => suggestion.ProjectName);

            private IEnumerable<string> tasksNames(IEnumerable<AutocompleteSuggestion> autocompleteSuggestions)
                => autocompleteSuggestions.Cast<TaskSuggestion>().Select(suggestion => suggestion.Name);

            [Fact, LogIfTooSlow]
            public async Task PrependsEmptyProjectToEveryGroup()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.AddRange(getProjectSuggestions(3, 0));
                suggestions.AddRange(getProjectSuggestions(4, 1));
                suggestions.AddRange(getProjectSuggestions(1, 10));
                suggestions.AddRange(getProjectSuggestions(10, 54));
                var suggestionsObservable = Observable.Return(suggestions);
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestionsObservable);
                ViewModel.Prepare();

                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                await ViewModel.Initialize();

                TestScheduler.Start();
                foreach (var group in observer.LastEmittedValue())
                {
                    group.Items.Cast<ProjectSuggestion>().First().ProjectName.Should().Be(Resources.NoProject);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task IsClearedWhenThereAreNoWordsToQuery()
            {
                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("", 0));

                TestScheduler.Start();
                observer.LastEmittedValue().Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSuggestAnythingWhenAProjectIsAlreadySelected()
            {
                var description = "abc";
                var projectA = Substitute.For<IThreadSafeProject>();
                projectA.Id.Returns(ProjectId);
                var projectB = Substitute.For<IThreadSafeProject>();
                projectB.Id.Returns(ProjectId + 1);
                var timeEntryA = Substitute.For<IThreadSafeTimeEntry>();
                timeEntryA.Description.Returns(description);
                timeEntryA.Project.Returns(projectA);
                var timeEntryB = Substitute.For<IThreadSafeTimeEntry>();
                timeEntryB.Description.Returns(description);
                timeEntryB.Project.Returns(projectB);

                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                var suggestions = Observable.Return(new AutocompleteSuggestion[]
                    {
                        new TimeEntrySuggestion(timeEntryA),
                        new TimeEntrySuggestion(timeEntryB)
                    });
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestions);

                ViewModel.Prepare(DefaultParameter);

                ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan(description, description.Length),
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor)
                );
                TestScheduler.Start();

                var result = observer.LastEmittedValue().ToList();
                result.Should().HaveCount(1);
                result[0].Items.Should().HaveCount(1);
                var suggestion = result[0].Items[0];
                suggestion.Should().BeOfType<TimeEntrySuggestion>();
                ((TimeEntrySuggestion)suggestion).ProjectId.Should().Be(ProjectId);
            }

            [Fact, LogIfTooSlow]
            public async Task SortsProjectsByName()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.Add(getProjectSuggestion(3, 0));
                suggestions.Add(getProjectSuggestion(4, 1));
                suggestions.Add(getProjectSuggestion(1, 0));
                suggestions.Add(getProjectSuggestion(33, 1));
                suggestions.Add(getProjectSuggestion(10, 1));
                var suggestionsObservable = Observable.Return(suggestions);
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestionsObservable);

                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                TestScheduler.Start();

                var result = observer.LastEmittedValue().ToList();
                result.Should().HaveCount(2);
                projectNames(result[0].Items).Should().BeInAscendingOrder();
                projectNames(result[1].Items).Should().BeInAscendingOrder();
            }

            public async Task SortsTasksByName()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.Add(getProjectSuggestion(3, 0, new[]
                {
                    new MockTask { Id = 2, WorkspaceId = 0, ProjectId = 3, Name = "Task2" },
                    new MockTask { Id = 1, WorkspaceId = 0, ProjectId = 3, Name = "Task1" },
                    new MockTask { Id = 3, WorkspaceId = 0, ProjectId = 3, Name = "Task3" }
                }));
                var suggestionsObservable = Observable.Return(suggestions);
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestionsObservable);

                var observer = TestScheduler
                    .CreateObserver<IEnumerable<SectionModel<string, AutocompleteSuggestion>>>();
                ViewModel.Suggestions.Subscribe(observer);

                ViewModel.Prepare();
                await ViewModel.Initialize();

                TestScheduler.Start();

                var result = observer.LastEmittedValue().ToList();
                ViewModel.ToggleTasks.Execute((ProjectSuggestion)result[0].Items[1]);
                TestScheduler.Start();

                result = observer.LastEmittedValue().ToList();
                result.Should().HaveCount(1);
                result[0].Items.Should().HaveCount(5);
                tasksNames(result[0].Items.Skip(2)).Should().BeInAscendingOrder();
            }
        }

        public sealed class TheTextFieldInfoProperty : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task DoesNotChangeSuggestionsWhenOnlyTheCursorMovesForward(string text)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, text.Length));
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));

                TestScheduler.Start();
                await AutocompleteProvider.DidNotReceive().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task ChangesSuggestionsWhenTheCursorMovesBackBehindTheOldCursorPosition(string text)
            {
                ViewModel.Suggestions.Subscribe();

                var extendedText = text + "x";
                ViewModel.Prepare(DefaultParameter);
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, text.Length));
                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, 0));
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, extendedText.Length));
                TestScheduler.Start();

                await AutocompleteProvider.Received().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task ChangesSuggestionsWhenTheCursorMovesBeforeTheQuerySymbolAndUserStartsTyping(string text)
            {
                ViewModel.Suggestions.Subscribe();

                ViewModel.Prepare(DefaultParameter);
                ViewModel.SetTextSpans.ExecuteSequentally(
                    new List<ISpan> { new QueryTextSpan(text, text.Length) },
                    new List<ISpan> { new QueryTextSpan(text, 0) }
                ).Subscribe();

                TestScheduler.Start();
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.SetTextSpans.Execute(new List<ISpan> { new QueryTextSpan("x" + text, 1) });

                TestScheduler.Start();
                await AutocompleteProvider.Received().Query(Arg.Is<QueryInfo>(query => query.Text.StartsWith("x")));
            }
        }

        public sealed class TheNoTagsInfoMessage : StartTimeEntryViewModelTest
        {

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task AppearsWhenSuggestingTagsAndUserHasNoTags(string query)
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan($"{QuerySymbols.Tags}{query}", 1)
                );

                TestScheduler.Start();
                var items = observer.LastEmittedValue().SelectMany(s => s.Items);
                var noEntity = items.Where(i => i is NoEntityInfoMessage).Select(i => i as NoEntityInfoMessage);
                noEntity.Should().Contain(i => i.Text == Resources.NoTagsInfoMessage);
            }

            [Theory, LogIfTooSlow]
            [InlineData(1, " ")]
            [InlineData(2, "#tag")]
            [InlineData(8, "@Project")]
            [InlineData(3, "Time entry")]
            [InlineData(1, "#")]
            public async Task DoesntAppearInAnyOtherCase(int tagCount, string query)
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                var tags = Enumerable
                    .Range(0, tagCount)
                    .Select(_ => Substitute.For<IThreadSafeTag>());
                DataSource.Tags.GetAll().Returns(Observable.Return(tags));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan(query, 1)
                );

                TestScheduler.Start();
                var items = observer.LastEmittedValue().SelectMany(s => s.Items);
                var noEntity = items.Where(i => i is NoEntityInfoMessage).Select(i => i as NoEntityInfoMessage);
                noEntity.Should().NotContain(i => i.Text == Resources.NoTagsInfoMessage);
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task DoesntAppearAfterCreatingATag(string query)
            {
                var observer = TestScheduler.CreateObserver<CollectionSections>();
                ViewModel.Suggestions.Subscribe(observer);

                var tag = Substitute.For<IThreadSafeTag>();
                InteractorFactory.CreateTag(Arg.Any<string>(), Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(tag));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();

                ViewModel.SetTextSpans.ExecuteWithCompletion(
                        new List<ISpan> { new QueryTextSpan($"{QuerySymbols.Tags}{query}", 1) }
                    )
                    .PrependAction(ViewModel.SelectSuggestion, new TagSuggestion(tag))
                    .Subscribe();

                TestScheduler.Start();
                var items = observer.LastEmittedValue().SelectMany(s => s.Items);
                var noEntity = items.Where(i => i is NoEntityInfoMessage).Select(i => i as NoEntityInfoMessage);
                noEntity.Should().NotContain(i => i.Text == Resources.NoTagsInfoMessage);
            }
        }
    }

    public static class TestExtensions
    {
        public static IObservable<Unit> OnTextFieldInfoFromView(this StartTimeEntryViewModel viewModel, params ISpan[] spans)
            => viewModel.SetTextSpans.ExecuteWithCompletion(spans.ToImmutableList());

        public static QueryTextSpan GetQuerySpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<QueryTextSpan>().Single();

        public static ProjectSpan GetProjectSpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<ProjectSpan>().Single();

        public static TagSpan GetTagSpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<TagSpan>().Single();

        public static TextSpan FirstTextSpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<TextSpan>().First();
    }
}

