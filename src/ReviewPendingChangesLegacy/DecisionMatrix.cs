using ReviewPendingChangesLegacy.Records;

namespace ReviewPendingChangesLegacy;

internal class DecisionMatrix
{
    public static DecisionType WhatToDo(GitStatus staged, GitStatus unStaged)
        => (staged, unStaged) switch
        {
            (GitStatus.Unmodified, GitStatus.Modified) => DecisionType.ReviewChanges,
            (GitStatus.Modified, GitStatus.Unmodified) => DecisionType.None,
            (GitStatus.Modified, GitStatus.Modified) => DecisionType.ReviewChanges, // TODO VRM review against what ?
            (GitStatus.Untracked, GitStatus.Untracked) => DecisionType.ReviewNewFile,
            (GitStatus.Added, GitStatus.Modified) => DecisionType.ReviewNewFile,
            (GitStatus.Added, GitStatus.Unmodified) => DecisionType.None,
            (GitStatus.Deleted, GitStatus.Unmodified) => DecisionType.None,
            (GitStatus.Renamed, GitStatus.Modified) => DecisionType.ReviewChanges,
            (GitStatus.Renamed, GitStatus.Unmodified) => DecisionType.None,
            (GitStatus.Unmodified, GitStatus.Deleted) => DecisionType.ReviewChanges,
            _ => DecisionType.Undefined,
        };

    public static Decision WhatToDo(FileStatus fileStatus) => new(fileStatus, DecisionMatrix.WhatToDo(fileStatus.Staged, fileStatus.UnStaged));
}