namespace ReviewPendingChanges

open System
open System.IO
open LibGit2Sharp

[<RequireQualifiedAccess>]
module Hijacker =

    let hijackSuffix = ".user.tmp"

    let isHijackFile (repo: Repository) (statusEntry: StatusEntry) =
        let fileInfo = repo.GetFileInfo(statusEntry)
        fileInfo.FullName.EndsWith(hijackSuffix, StringComparison.InvariantCultureIgnoreCase)

    let getHijackFilePath (repo: Repository) (statusEntry: StatusEntry) =
        let fileInfo = repo.GetFileInfo(statusEntry)
        let hijackFilePath = $"{fileInfo.FullName}{hijackSuffix}"
        FileInfo hijackFilePath

    let isHijacked (repo: Repository) (statusEntry: StatusEntry) =
        let hijackFileInfo = getHijackFilePath repo statusEntry
        hijackFileInfo.Exists

    let getHijackStatus repo statusEntry =
        match isHijacked repo statusEntry with
        | true -> HijackStatus.Hijacked
        | false -> HijackStatus.NotHijacked

    let hijack repo statusEntry =
        let hijackFileInfo = getHijackFilePath repo statusEntry

        if hijackFileInfo.Exists then
            failwith $"Hijack file already exists: {hijackFileInfo.FullName}"

        File.WriteAllBytes(hijackFileInfo.FullName, [||])

    let unHijack repo statusEntry =
        let hijackFileInfo = getHijackFilePath repo statusEntry

        if not hijackFileInfo.Exists then
            failwith $"Hijack file does not exist: {hijackFileInfo.FullName}"

        hijackFileInfo.Delete()
