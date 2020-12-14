using Moq;
using NUnit.Framework;
using ReviewPendingChanges.Records;

namespace ReviewPendingChanges.Tests
{
    public class GitHelperTests
    {
        [Test]
        public void TestParseGitStatus()
        {
            var mock = new Mock<IGitCaller>();
            mock.Setup(x => x.GetStatus()).Returns(
                new[]
                {
                    " M src/Program.cs",
                }
            );

            var sut = new GitHelper(mock.Object);
            var fileStatusEnumerable = sut.GetFilesStatus();
            var expected = new FileStatus[]
            {
                new(GitStatus.Unmodified, GitStatus.Modified, "src/Program.cs"),
            };

            CollectionAssert.AreEqual(expected, fileStatusEnumerable);
        }

        [Test]
        public void TestWhatToDo()
        {
            var input = new FileStatus(GitStatus.Unmodified, GitStatus.Modified, "src/Program.cs");
            var actual = DecisionMatrix.WhatToDo(input);
            var expected = new Decision(input, DecisionType.ReviewChanges);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(GitStatus.Unmodified, GitStatus.Modified, DecisionType.ReviewChanges)]
        public void TestWhatToDo(GitStatus staged, GitStatus unStaged, DecisionType expectedDecision) => Assert.AreEqual(expectedDecision, DecisionMatrix.WhatToDo(staged, unStaged));
    }
}
