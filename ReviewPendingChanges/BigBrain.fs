namespace ReviewPendingChanges

open LibGit2Sharp
open Pinicola.FSharp

[<RequireQualifiedAccess>]
module BigBrain =

    let isSubmodule (repo: Repository) (entry: StatusEntry) =
        repo.Submodules |> Seq.exists (fun sm -> sm.Path = entry.FilePath)

    let shouldBeReviewed repo (entry: StatusEntry) (status: FileStatus) =

        match status with
        | FileStatus.NewInWorkdir -> Some SimplifiedFileStatus.New
        | FileStatus.ModifiedInWorkdir -> Some SimplifiedFileStatus.Modified
        | FileStatus.DeletedFromWorkdir -> Some SimplifiedFileStatus.Deleted
        | FileStatus.RenamedInWorkdir -> Some SimplifiedFileStatus.Renamed

        // TODO : maybe review this later
        | FileStatus.TypeChangeInWorkdir when isSubmodule repo entry -> None

        | FileStatus.Unaltered
        | FileStatus.NewInIndex
        | FileStatus.ModifiedInIndex
        | FileStatus.DeletedFromIndex
        | FileStatus.RenamedInIndex
        | FileStatus.Ignored -> None

        | FileStatus.TypeChangeInIndex
        | FileStatus.Unreadable
        | FileStatus.Nonexistent
        | FileStatus.Conflicted -> failwith $"Unexpected status '%A{status}' for file: %s{entry.FilePath}"

        | s -> failwith $"Unknown status %A{s} for file: %s{entry.FilePath}"

    let tryGetStatusToConsider (repo: Repository) (entry: StatusEntry) =

        let fileStatusList =
            entry.State |> Enum.getFlags |> List.choose (shouldBeReviewed repo entry)

        match fileStatusList with
        | [] -> None
        | [ s ] ->
            let hijackStatus = Hijacker.getHijackStatus repo entry
            Some(s, hijackStatus)
        | xs -> failwith $"Multiple relevant statuses found for file: {entry.FilePath}: %A{xs}"

    let getStatusToConsider repo entry =
        match tryGetStatusToConsider repo entry with
        | Some s -> s
        | None -> failwith $"No status to consider found for file: {entry.FilePath}"

    let hasStatusToConsider repo entry =
        match tryGetStatusToConsider repo entry with
        | None -> false
        | Some _ -> true

    let filterToReview repo (changes: StatusEntry list) =

        changes |> List.filter (hasStatusToConsider repo)

    let getActionsForStatus simplifiedStatus hijackStatus =

        let autoAction =
            match simplifiedStatus with
            | SimplifiedFileStatus.New -> AutoAction.OpenInEditor
            | SimplifiedFileStatus.Modified -> AutoAction.OpenInDiffTool
            | SimplifiedFileStatus.Deleted -> AutoAction.Noop
            | SimplifiedFileStatus.Renamed -> AutoAction.Noop
            | SimplifiedFileStatus.SubModule -> AutoAction.Noop

        let userPossibleActions =
            match hijackStatus, simplifiedStatus with
            | HijackStatus.Hijacked, _ -> [
                UserPossibleAction.UnHijack
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]
            | HijackStatus.NotHijacked, SimplifiedFileStatus.New -> [
                UserPossibleAction.Stage
                UserPossibleAction.Delete
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]
            | HijackStatus.NotHijacked, SimplifiedFileStatus.Modified -> [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Hijack
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]
            | HijackStatus.NotHijacked, SimplifiedFileStatus.Deleted -> [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]
            | HijackStatus.NotHijacked, SimplifiedFileStatus.Renamed -> [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]
            | HijackStatus.NotHijacked, SimplifiedFileStatus.SubModule -> [
                UserPossibleAction.Stage
                UserPossibleAction.Discard
                UserPossibleAction.Ignore
                UserPossibleAction.Restart
              ]

        autoAction, userPossibleActions
