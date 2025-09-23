namespace ReviewPendingChanges

open LibGit2Sharp
open Pinicola.FSharp

[<RequireQualifiedAccess>]
module BigBrain =
    let shouldBeReviewed filePath (status: FileStatus) =
        match status with
        | FileStatus.NewInWorkdir -> Some SimplifiedFileStatus.New
        | FileStatus.ModifiedInWorkdir -> Some SimplifiedFileStatus.Modified
        | FileStatus.DeletedFromWorkdir -> Some SimplifiedFileStatus.Deleted
        | FileStatus.RenamedInWorkdir -> Some SimplifiedFileStatus.Renamed

        | FileStatus.Unaltered
        | FileStatus.NewInIndex
        | FileStatus.ModifiedInIndex
        | FileStatus.DeletedFromIndex
        | FileStatus.RenamedInIndex
        | FileStatus.Ignored -> None

        | FileStatus.TypeChangeInIndex -> failwith $"Unexpected type change in index for file: %s{filePath}"
        | FileStatus.TypeChangeInWorkdir -> failwith $"Unexpected type change in workdir for file: %s{filePath}"
        | FileStatus.Unreadable -> failwith $"Unexpected unreadable file: %s{filePath}"
        | FileStatus.Nonexistent -> failwith $"Unexpected non existent file: %s{filePath}"
        | FileStatus.Conflicted -> failwith $"Unexpected conflicted file: %s{filePath}"

        | s -> failwith $"Unknown status %A{s} for file: %s{filePath}"

    let tryGetStatusToConsider (entry: StatusEntry) =
        let fileStatusList =
            entry.State |> Enum.getFlags |> List.choose (shouldBeReviewed entry.FilePath)

        match fileStatusList with
        | [] -> None
        | [ s ] -> Some s
        | xs -> failwith $"Multiple relevant statuses found for file: {entry.FilePath}: %A{xs}"

    let getStatusToConsider entry =
        match tryGetStatusToConsider entry with
        | Some s -> s
        | None -> failwith $"No status to consider found for file: {entry.FilePath}"

    let hasStatusToConsider entry =
        match tryGetStatusToConsider entry with
        | None -> false
        | Some _ -> true

    let filterToReview (changes: StatusEntry list) =

        changes |> List.filter hasStatusToConsider

    let getActionsForStatus status =
        match status with
        | SimplifiedFileStatus.New ->
            AutoAction.OpenInEditor,
            [
                UserPossibleAction.Stage
                UserPossibleAction.Delete
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
            ]
        | SimplifiedFileStatus.Modified ->
            AutoAction.OpenInDiffTool,
            [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Hijack
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
            ]
        | SimplifiedFileStatus.Deleted ->
            AutoAction.Noop,
            [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
            ]
        | SimplifiedFileStatus.Renamed ->
            AutoAction.Noop,
            [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
            ]
