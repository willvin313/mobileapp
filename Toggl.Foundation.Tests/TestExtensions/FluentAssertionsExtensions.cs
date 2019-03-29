using FluentAssertions;
using FluentAssertions.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class FluentAssertionsExtensions
    {
        public static AndConstraint<TAssertions> BeSequenceEquivalentTo<TExpectation, TSubject, TAssertions>(
            this CollectionAssertions<TSubject, TAssertions> collectionAssertions,
            IEnumerable<TExpectation> expectation,
            string because = "",
            params object[] becauseArgs)
            where TSubject : IEnumerable
            where TAssertions : CollectionAssertions<TSubject, TAssertions>
        {
            return collectionAssertions.BeEquivalentTo(expectation, options => options.WithStrictOrdering(), because, becauseArgs);
        }
    }
}
