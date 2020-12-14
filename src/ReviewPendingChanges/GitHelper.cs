using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ReviewPendingChanges.Records;

namespace ReviewPendingChanges
{
    internal class GitHelper
    {
        private readonly IGitCaller _gitCaller;
        private readonly Lazy<IDictionary<string, GitStatus>> _gitStatusMap = new(Helpers.MapEnumMembers<GitStatus>, LazyThreadSafetyMode.ExecutionAndPublication);

        public GitHelper(IGitCaller gitCaller)
        {
            _gitCaller = gitCaller;
        }

        public IEnumerable<FileStatus> GetFilesStatus() =>
            _gitCaller.GetStatus()
                .Select(
                    line => new FileStatus(
                        _gitStatusMap.Value[line.Substring(0, 1)],
                        _gitStatusMap.Value[line.Substring(1, 1)],
                        line.Substring(3)
                    )
                );

        public void DiffTool(Decision file)
        {
            if (file.DecisionType == DecisionType.ReviewNewFile)
            {
                _gitCaller.NewFileDiff(file.FileStatus.File);
            }
            else
            {
                _gitCaller.DiffTool(file.FileStatus.File);
            }
        }

        public IEnumerable<UserFeedback> GetActions(DecisionType decisionDecisionType) =>
            decisionDecisionType switch
            {
                DecisionType.ReviewChanges => new[]
                {
                    UserFeedback.DiscardChanges,
                    UserFeedback.Stage,
                    UserFeedback.Relaunch,
                },
                DecisionType.ReviewNewFile => new[]
                {
                    UserFeedback.DiscardChanges,
                    UserFeedback.Stage,
                    UserFeedback.Relaunch,
                },
                _ => throw new ArgumentOutOfRangeException(nameof(decisionDecisionType), decisionDecisionType, null),
            };

        public void PerformAction(UserFeedback userFeedback, FileStatus file) =>
            ((Action<string>)(userFeedback switch
            {
                UserFeedback.Stage => _gitCaller.Add,
                UserFeedback.DiscardChanges => _gitCaller.Discard,
                UserFeedback.Relaunch => Noop,
                _ => throw new ArgumentOutOfRangeException(nameof(userFeedback), userFeedback, null),
            }))(file.File);

        public bool NeedConfirmation(UserFeedback userFeedback) =>
            userFeedback switch
            {
                UserFeedback.DiscardChanges => true,
                _ => false,
            };

        private void Noop(string _) { }
    }
}
