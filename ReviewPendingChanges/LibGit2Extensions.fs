namespace ReviewPendingChanges

open System.IO
open System.Runtime.CompilerServices
open LibGit2Sharp

type LibGit2Extensions =

    [<Extension>]
    static member GetFileInfo(repo: Repository, statusEntry: StatusEntry) : FileInfo =
        let repoPath = repo.Info.WorkingDirectory
        let fileRelativePath = statusEntry.FilePath
        let fullPath = Path.Join(repoPath, fileRelativePath)
        FileInfo fullPath
