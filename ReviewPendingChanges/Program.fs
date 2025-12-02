open System
open System.Diagnostics
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

    let getToReview ignoredFiles =
        repo.RetrieveStatus()
        |> Seq.toList
        |> BigBrain.filterToReview repo
        |> List.filter (fun e -> not (ignoredFiles |> List.contains e.FilePath))

    let runCommand (cmd: string) (args: string list) workingDir waitForExit =
        let psi = ProcessStartInfo(cmd, args)
        psi.WorkingDirectory <- workingDir
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        let p = new Process(StartInfo = psi)
        let started = p.Start()

        if not started then
            failwith $"Failed to start command: {cmd} with args: {args}"

        if waitForExit then
            p.WaitForExit()

    let runCommandAndWait cmd args workingDir = runCommand cmd args workingDir true
    let runCommand cmd args workingDir = runCommand cmd args workingDir false

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
        runCommandAndWait
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
        let simplifiedStatus, hijackStatus = BigBrain.getStatusToConsider repo entry

        let autoAction, userPossibleActions =
            BigBrain.getActionsForStatus simplifiedStatus hijackStatus

        runAutoAction autoAction entry

        let prefix, color =
            match simplifiedStatus with
            | SimplifiedFileStatus.New -> "[+]", "green"
            | SimplifiedFileStatus.Deleted -> "-", "red"
            | SimplifiedFileStatus.Modified -> "~", "yellow"
            | SimplifiedFileStatus.Renamed -> ">", "blue"
            | SimplifiedFileStatus.SubModule -> "🗁", "magenta"

        let hijackStatusText =
            match hijackStatus with
            | HijackStatus.NotHijacked -> Raw ""
            | HijackStatus.Hijacked -> Markup " [red](Hijacked)[/] "

        let filePrefix =
            SpectreConsoleString.build [
                SpectreConsoleString.fromInterpolated $"[grey][[{counter.ToString().PadLeft(padding)}/{total}]][/] "
                Markup $"[{color}]{prefix |> Markup.escape}[/] "
                hijackStatusText
                SpectreConsoleString.fromInterpolated $"[yellow]{entry.FilePath}[/] "
            ]

        let continue1, ignoredFiles =
            match hijackStatus with
            | HijackStatus.NotHijacked -> true, ignoredFiles
            | HijackStatus.Hijacked ->

                let hijackPossibleChoices = [
                    UserPossibleAction.UnHijack
                    UserPossibleAction.Ignore
                    UserPossibleAction.Restart
                ]

                let hijackPromptTitle =
                    SpectreConsoleString.build [
                        filePrefix
                        Markup $" [red](Hijacked)[/]"
                    ]

                let hijackChoice =
                    SelectionPrompt.init ()
                    |> SelectionPrompt.withTitle hijackPromptTitle
                    |> SelectionPrompt.addChoices hijackPossibleChoices
                    |> SelectionPrompt.withWrapAround true
                    |> AnsiConsole.prompt

                match hijackChoice with
                | UserPossibleAction.UnHijack ->
                    Hijacker.unHijack repo entry
                    true, ignoredFiles
                | UserPossibleAction.Ignore -> false, entry.FilePath :: ignoredFiles
                | UserPossibleAction.Restart -> false, ignoredFiles
                | _ -> failwith "Unexpected choice in hijack handling"

        if not continue1 then
            ignoredFiles
        else

            let selectionPromptTitle =
                SpectreConsoleString.build [
                    filePrefix
                    SpectreConsoleString.fromInterpolated $"[grey]({simplifiedStatus} >> {autoAction})[/]"
                ]

            let selectionPrompt =
                SelectionPrompt.init ()
                |> SelectionPrompt.withTitle selectionPromptTitle
                |> SelectionPrompt.addChoices userPossibleActions
                |> SelectionPrompt.withWrapAround true

            let userChoice = AnsiConsole.prompt selectionPrompt

            let needsConfirmation =
                match userChoice with
                | UserPossibleAction.Discard
                | UserPossibleAction.Delete -> true
                | UserPossibleAction.Restart
                | UserPossibleAction.Ignore
                | UserPossibleAction.Stage
                | UserPossibleAction.Hijack
                | UserPossibleAction.UnHijack -> false

            let shouldContinue =
                if needsConfirmation then
                    let confirmText =
                        SpectreConsoleString.build [
                            filePrefix
                            SpectreConsoleString.fromInterpolated
                                $"Are you sure you want to [red]{userChoice}[/] this file ?"
                        ]

                    SelectionPrompt.init ()
                    |> SelectionPrompt.withTitle confirmText
                    |> SelectionPrompt.addChoices Maybe.all
                    |> SelectionPrompt.withWrapAround true
                    |> AnsiConsole.prompt
                    |> Maybe.toBool

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
                        Hijacker.hijack repo entry
                        // If the file is hijacked, we should ignore it in the next review loop
                        entry.FilePath :: ignoredFiles

                    | UserPossibleAction.UnHijack ->
                        let hijackFileInfo = Hijacker.getHijackFilePath repo entry

                        if not hijackFileInfo.Exists then
                            failwith $"Hijack file does not exist: {hijackFileInfo.FullName}"

                        hijackFileInfo.Delete()

                        if hijackFileInfo.Exists then
                            failwith $"Failed to delete hijack file: {hijackFileInfo.FullName}"

                        ignoredFiles

                    | UserPossibleAction.Delete ->
                        Commands.Remove(repo, entry.FilePath)
                        ignoredFiles

            AnsiConsole.writeLine
            <| SpectreConsoleString.build [
                filePrefix
                SpectreConsoleString.fromInterpolated $"[green]{userChoice |> UserPossibleAction.asPastAction}[/]"
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
