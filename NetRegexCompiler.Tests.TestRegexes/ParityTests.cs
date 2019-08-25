using System;
using System.Linq;
using System.Text.RegularExpressions;
using NetRegexCompiler.Tests.CompiledTestRegexes;
using Xunit;
using Xunit.Sdk;

namespace NetRegexCompiler.Tests
{
    public sealed class ParityTests
    {
        [Theory, MemberData(nameof(Theories.RegexTests), MemberType = typeof(Theories))]
        public void Properties(Regex compiledRegex, RegexOptions options, string _, string compiledRegexId)
        {
            Assert.Equal(options, compiledRegex.Options);
            var regex = new Regex(compiledRegex.ToString(), compiledRegex.Options);
            Assert.Equal(regex.Options, compiledRegex.Options);
            Assert.Equal(regex.RightToLeft, compiledRegex.RightToLeft);
            Assert.Equal(regex.GetGroupNames(), compiledRegex.GetGroupNames());
            Assert.Equal(regex.GetGroupNumbers(), compiledRegex.GetGroupNumbers());
            Assert.Equal(regex.GetGroupNames().Select(regex.GroupNumberFromName), compiledRegex.GetGroupNames().Select(compiledRegex.GroupNumberFromName));
            Assert.Equal(regex.GetGroupNumbers().Select(regex.GroupNameFromNumber), compiledRegex.GetGroupNumbers().Select(compiledRegex.GroupNameFromNumber));
        }

        [Theory, MemberData(nameof(Theories.RegexTests), MemberType = typeof(Theories))]
        public void IsMatch(Regex compiledRegex, RegexOptions options, string testString, string compiledRegexId)
        {
            Assert.Equal(new Regex(compiledRegex.ToString(), compiledRegex.Options).IsMatch(testString), compiledRegex.IsMatch(testString));
        }

        [Theory, MemberData(nameof(Theories.RegexTests), MemberType = typeof(Theories))]
        public void Match(Regex compiledRegex, RegexOptions options, string testString, string compiledRegexId)
        {
            var expectedMatch = new Regex(compiledRegex.ToString(), compiledRegex.Options).Match(testString);
            var compiledMatch = compiledRegex.Match(testString);

            Assert.Equal(expectedMatch.Success, compiledMatch.Success);
            Assert.Equal(expectedMatch.Index, compiledMatch.Index);
            Assert.Equal(expectedMatch.Length, compiledMatch.Length);
            Assert.Equal(expectedMatch.Name, compiledMatch.Name);
            Assert.Equal(expectedMatch.Value, compiledMatch.Value);
            Assert.Equal(expectedMatch.Groups.Count, compiledMatch.Groups.Count);
            Assert.Collection(compiledMatch.Groups, expectedMatch.Groups.Select<Group, Action<Group>>(expected => actual =>
            {
                Assert.Equal(expected.Success, actual.Success);
                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Length, actual.Length);
                Assert.Equal(expected.Name, actual.Name);
                Assert.Equal(expected.Value, actual.Value);
            }).ToArray());
            Assert.Collection(compiledMatch.Captures, expectedMatch.Captures.Select<Capture, Action<Capture>>(expected => actual =>
            {
                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Length, actual.Length);
                Assert.Equal(expected.Value, actual.Value);
            }).ToArray());
        }

        [Theory, MemberData(nameof(Theories.RegexTests), MemberType = typeof(Theories))]
        public void Matches(Regex compiledRegex, RegexOptions options, string testString, string compiledRegexId)
        {
            var expectedMatches = new Regex(compiledRegex.ToString(), compiledRegex.Options).Matches(testString);
            var compiledMatches = compiledRegex.Matches(testString);

            Assert.Equal(expectedMatches.Count, compiledMatches.Count);

            Assert.Collection(compiledMatches, expectedMatches.Select<Match, Action<Match>>(expectedMatch => actualMatch =>
            {
                Assert.Equal(expectedMatch.Success, actualMatch.Success);
                Assert.Equal(expectedMatch.Index, actualMatch.Index);
                Assert.Equal(expectedMatch.Length, actualMatch.Length);
                Assert.Equal(expectedMatch.Name, actualMatch.Name);
                Assert.Equal(expectedMatch.Value, actualMatch.Value);
                Assert.Equal(expectedMatch.Groups.Count, actualMatch.Groups.Count);
                Assert.Collection(actualMatch.Groups, expectedMatch.Groups.Select<Group, Action<Group>>(expected => actual =>
                {
                    Assert.Equal(expected.Success, actual.Success);
                    Assert.Equal(expected.Index, actual.Index);
                    Assert.Equal(expected.Length, actual.Length);
                    Assert.Equal(expected.Name, actual.Name);
                    Assert.Equal(expected.Value, actual.Value);
                }).ToArray());
                Assert.Collection(actualMatch.Captures, expectedMatch.Captures.Select<Capture, Action<Capture>>(expected => actual =>
                {
                    Assert.Equal(expected.Index, actual.Index);
                    Assert.Equal(expected.Length, actual.Length);
                    Assert.Equal(expected.Value, actual.Value);
                }).ToArray());
            }).ToArray());
        }
    }
}