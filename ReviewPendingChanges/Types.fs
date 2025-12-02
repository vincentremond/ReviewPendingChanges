namespace ReviewPendingChanges

[<RequireQualifiedAccess>]
type Maybe =
    | Yes
    | No

    static member all = [
        Yes
        No
    ]

    static member toBool =
        function
        | Yes -> true
        | No -> false

[<RequireQualifiedAccess>]
type SimplifiedFileStatus =
    | Modified
    | New
    | Deleted
    | Renamed
    | SubModule

[<RequireQualifiedAccess>]
type HijackStatus =
    | NotHijacked
    | Hijacked

[<RequireQualifiedAccess>]
type AutoAction =
    | OpenInEditor
    | OpenInDiffTool
    | Noop

[<RequireQualifiedAccess>]
type UserPossibleAction =
    | Stage
    | Hijack
    | UnHijack
    | Discard
    | Ignore
    | Restart
    | Delete

    static member asPastAction =
        function
        | UserPossibleAction.Stage -> "Staged"
        | UserPossibleAction.Hijack -> "Hijacked"
        | UserPossibleAction.UnHijack -> "Unhijacked"
        | UserPossibleAction.Discard -> "Discarded"
        | UserPossibleAction.Ignore -> "Ignored"
        | UserPossibleAction.Restart -> "Restarted"
        | UserPossibleAction.Delete -> "Deleted"
