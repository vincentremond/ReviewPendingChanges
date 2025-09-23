open System

open System.Diagnostics
open System.IO
open LibGit2Sharp
open Pinicola.FSharp.SpectreConsole
open ReviewPendingChanges

AnsiConsole.markupLine "[blue][[ReviewPendingChanges]][/]"

let currentDirectory = Environment.CurrentDirectory

AnsiConsole.markupLine $"[grey]Current directory: {currentDirectory}[/]"

let gitDirectory = Repository.Discover(currentDirectory) |> Option.ofObj

match gitDirectory with
| None -> AnsiConsole.markupLine "[red]No git repository found in the current directory or its parents.[/]"
| Some gitDirectory ->
    AnsiConsole.markupLine $"[grey]Git repository found at: {gitDirectory}[/]"
    let repo = new Repository(gitDirectory)

    let hijackSuffix = ".user.tmp"

    let getHijackFilePath (entry: StatusEntry) =
        let repoPath = repo.Info.WorkingDirectory
        let hijackRelativePath = $"{entry.FilePath}{hijackSuffix}"
        let hijackFileInfo = Path.Join(repoPath, hijackRelativePath) |> FileInfo
        hijackFileInfo

    let checkIfHijack (fileEntry: StatusEntry) =

        fileEntry.FilePath.EndsWith(hijackSuffix, StringComparison.InvariantCultureIgnoreCase)
        || (getHijackFilePath fileEntry).Exists

    let getToReview ignoredFiles =
        repo.RetrieveStatus()
        |> Seq.toList
        |> BigBrain.filterToReview
        |> List.filter (fun e -> not (ignoredFiles |> List.contains e.FilePath))
        |> List.filter (checkIfHijack >> not)

    let runCommand (cmd: string) (args: string list) workingDir =
        let psi = ProcessStartInfo(cmd, args)
        psi.WorkingDirectory <- workingDir
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        let p = new Process(StartInfo = psi)
        let started = p.Start()

        if not started then
            failwith $"Failed to start command: {cmd} with args: {args}"

    let runAutoAction action (entry: StatusEntry) =
        match action with
        | AutoAction.Noop -> ()
        | AutoAction.OpenInDiffTool ->
            runCommand
                "git"
                [
                    "difftool"
                    "-y"
                    entry.FilePath
                ]
                repo.Info.WorkingDirectory
        | AutoAction.OpenInEditor ->
            runCommand
                "cmd.exe"
                [
                    "/c"
                    "code"
                    repo.Info.WorkingDirectory
                    "--goto"
                    entry.FilePath
                ]
                repo.Info.WorkingDirectory

    let discardFile (repo: Repository) filePath =
        runCommand
            "git"
            [
                "checkout"
                "--"
                filePath
            ]
            repo.Info.WorkingDirectory

    let doReviewChangeForFile entry counter otherCount ignoredFiles =
        let total = counter + otherCount
        let padding = total.ToString().Length
        let statusToConsider = BigBrain.getStatusToConsider entry
        let autoAction, userPossibleActions = BigBrain.getActionsForStatus statusToConsider
        runAutoAction autoAction entry

        let prefix, color =
            match statusToConsider with
            | SimplifiedFileStatus.New -> "+", "green"
            | SimplifiedFileStatus.Deleted -> "-", "red"
            | SimplifiedFileStatus.Modified -> "~", "yellow"
            | SimplifiedFileStatus.Renamed -> ">", "blue"

        let filePrefix =
            SpectreConsoleString.build [
                SpectreConsoleString.fromInterpolated $"[grey][[{counter.ToString().PadLeft(padding)}/{total}]][/] "
                Markup $"[{color}]{prefix}[/] "
                SpectreConsoleString.fromInterpolated $"[yellow]{entry.FilePath}[/] "
            ]

        let title =
            SpectreConsoleString.build [
                filePrefix
                SpectreConsoleString.fromInterpolated $"[grey]({statusToConsider} >> {autoAction})[/]"
            ]

        let selectionPrompt =
            SelectionPrompt.init ()
            |> SelectionPrompt.withTitle title
            |> SelectionPrompt.addChoices userPossibleActions

        let userChoice = AnsiConsole.prompt selectionPrompt

        let needsConfirmation =
            match userChoice with
            | UserPossibleAction.Discard
            | UserPossibleAction.Delete -> true
            | UserPossibleAction.Restart
            | UserPossibleAction.Ignore
            | UserPossibleAction.Stage
            | UserPossibleAction.Hijack -> false

        let shouldContinue =
            if needsConfirmation then
                let confirmText =
                    SpectreConsoleString.build [
                        filePrefix
                        SpectreConsoleString.fromInterpolated
                            $"Are you sure you want to [red]{userChoice}[/] this file ?"
                    ]

                AnsiConsole.confirm confirmText
            else
                true

        let result =
            if not shouldContinue then
                ignoredFiles
            else
                match userChoice with
                | UserPossibleAction.Restart -> ignoredFiles
                | UserPossibleAction.Ignore -> entry.FilePath :: ignoredFiles
                | UserPossibleAction.Stage ->
                    Commands.Stage(repo, entry.FilePath)
                    ignoredFiles
                | UserPossibleAction.Discard ->
                    discardFile repo entry.FilePath
                    ignoredFiles
                | UserPossibleAction.Hijack ->
                    let hijackFileInfo = getHijackFilePath entry
                    if hijackFileInfo.Exists then
                        failwith $"Hijack file already exists: {hijackFileInfo.FullName}"

                    File.WriteAllBytes(hijackFileInfo.FullName, [||])
                    ignoredFiles

                | UserPossibleAction.Delete ->
                    Commands.Remove(repo, entry.FilePath)
                    ignoredFiles

        AnsiConsole.writeLine
        <| SpectreConsoleString.build [
            filePrefix
            SpectreConsoleString.fromInterpolated $"[green]{userChoice}[/]"
        ]

        result

    let rec doReviewLoop counter ignoredFiles =
        let toReview = getToReview ignoredFiles

        match toReview with
        | [] -> AnsiConsole.markupLineInterpolated $"[green]Nothing to review[/]"
        | r :: rem ->
            let newIgnoredFiles = doReviewChangeForFile r counter rem.Length ignoredFiles
            doReviewLoop (counter + 1) newIgnoredFiles

    doReviewLoop 1 []
