namespace ReviewPendingChangesLegacy.Records;

internal record FileStatus(GitStatus Staged, GitStatus UnStaged, string File);