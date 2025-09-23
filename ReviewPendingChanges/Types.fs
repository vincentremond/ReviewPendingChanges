namespace ReviewPendingChanges

[<RequireQualifiedAccess>]
type SimplifiedFileStatus =
    | Modified
    | New
    | Deleted
    | Renamed

[<RequireQualifiedAccess>]
type AutoAction =
    | OpenInEditor
    | OpenInDiffTool
    | Noop

[<RequireQualifiedAccess>]
type UserPossibleAction =
    | Stage
    | Hijack
    | Discard
    | Ignore
    | Restart
    | Delete
