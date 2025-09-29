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
