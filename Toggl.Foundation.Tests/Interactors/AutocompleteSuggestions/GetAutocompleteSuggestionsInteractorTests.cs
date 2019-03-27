using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.AutocompleteSuggestions;
using Toggl.Foundation.Tests.Autocomplete;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.AutocompleteSuggestions
{
    public sealed class AutocompleteProviderTests
    {
        public class GetAutocompleteSuggestionsInteractorTests : BaseInteractorTests
        {
            protected const long WorkspaceId = 9;
            protected const long ProjectId = 10;
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";

            protected IInteractorFactory InteractorFactory { get; } = Substitute.For<IInteractorFactory>();

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task WhenTheUserBeginsTypingADescription(string description)
            {
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(description, 0));

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords()))
                    );
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task WhenTheUserHasTypedAnySearchSymbolsButMovedTheCaretToAnIndexThatComesBeforeTheSymbol(
                string description)
            {
                var actualDescription = $"{description} @{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, 0));

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(actualDescription.SplitToQueryWords())));
            }

            [Fact, LogIfTooSlow]
            public async Task WhenTheUserHasAlreadySelectedAProjectAndTypesTheAtSymbol()
            {
                var description = $"Testing Mobile Apps @toggl";
                var textFieldInfo = TextFieldInfo.Empty(1).ReplaceSpans(
                    new QueryTextSpan(description, description.Length),
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor)
                );

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task UsesGetProjectsAutocompleteSuggestionsInteractorWhenTheAtSymbolIsTyped(string description)
            {
                var actualDescription = $"{description} @{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, description.Length + 2));

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .Received()
                    .GetProjectsAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task UsesGetTagsAutocompleteSuggestionsInteractorWhenTheHashtagSymbolIsTyped(string description)
            {
                var actualDescription = $"{description} #{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, description.Length + 2));

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .Received()
                    .GetTagsAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotUseInteractorsWhenTheSarchStringIsEmpty()
            {
                var textFieldInfo = TextFieldInfo.Empty(1).ReplaceSpans(new QueryTextSpan());

                var interactor = new GetAutocompleteSuggestions(InteractorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                InteractorFactory
                    .DidNotReceive()
                    .GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>());
                InteractorFactory
                    .DidNotReceive()
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>());
                InteractorFactory
                    .DidNotReceive()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Any<IList<string>>());
            }
        }
    }
}
