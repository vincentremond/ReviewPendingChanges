namespace ReviewPendingChanges.Records;

internal record FileStatus(GitStatus Staged, GitStatus UnStaged, string File);