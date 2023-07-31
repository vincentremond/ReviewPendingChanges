﻿using System;
using System.IO;
using System.Linq;

namespace ReviewPendingChanges;

public class GitCaller : IGitCaller
{
    private readonly string _repository;

    public GitCaller(string repository)
    {
        _repository = repository;
    }

    public string[] GetStatus()
    {
        return SimpleGitCommand("ls-files --others --exclude-standard")
            .Select(f => $"?? {f}")
            .Union(SimpleGitCommand("status --porcelain"))
            .Distinct()
            .ToArray();
    }

    public void DiffTool(string file) => SimpleGitCommand($"-c diff.mnemonicprefix=false -c core.quotepath=false --no-optional-locks difftool -y \"{file}\"");
    public void Add(string file) => SimpleGitCommand($"add \"{file}\"");
    public void Discard(string file) => SimpleGitCommand($"checkout -- \"{file}\"");
    public void NewFileDiff(string fileStatusFile) => OpenTextEditor($"\"{Path.Combine(_repository, fileStatusFile.Replace('/', Path.DirectorySeparatorChar))}\"");
    public void Delete(string file) => File.Delete(file);

    private void OpenTextEditor(string path)
    {
        Logger.Verbose($"notepad {path}");
        ProcessHelper.StartAndWait(@"notepad.exe", _repository, path);
    }

    private string[] SimpleGitCommand(string arguments)
    {
        Logger.Verbose($"git {arguments}");

        return ProcessHelper.StartAndWait("git.exe", _repository, arguments)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}