using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ReviewPendingChangesLegacy.Records;

namespace ReviewPendingChangesLegacy;

internal class GitHelper
{
    private readonly IGitCaller _gitCaller;
    private readonly Lazy<IDictionary<string, GitStatus>> _gitStatusMap = new(Helpers.MapEnumMembers<GitStatus>, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Regex _regexRename = new Regex("^(?<OldName>.+)\\ ->\\ (?<NewName>.+)$", RegexOptions.Compiled);
    private readonly List<string> _ignoreList;

    public GitHelper(IGitCaller gitCaller)
    {
        _gitCaller = gitCaller;
        _ignoreList = new List<string>();
    }

    public IEnumerable<FileStatus> GetFilesStatus() =>
        _gitCaller.GetStatus()
            .Select(
                line => new FileStatus(
                    _gitStatusMap.Value[line.Substring(0, 1)],
                    _gitStatusMap.Value[line.Substring(1, 1)],
                    GetFileValue(line.Substring(3)).Trim('"')
                )
            )
            .Where(status => !_ignoreList.Contains(status.File));

    private static string GetFileValue(string line)
    {
        var fileValue = GetPath(line);
        return FixOctalEncoding(fileValue);
    }

    private static string FixOctalEncoding(string path)
    {
        var bytes = new List<byte>();
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] == '\\')
            {
                var octal = path.Substring(i + 1, 3);
                bytes.Add(Convert.ToByte(octal, 8));
                i += 3;
            }
            else if (path[i] < 0x80)
            {
                bytes.Add((byte)path[i]);
            }
            else
            {
                throw new InvalidOperationException($"Invalid character {path[i]}");
            }
        }

        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static string GetPath(string line)
    {
        var match = _regexRename.Match(line);
        if (match.Success)
        {
            return match.Groups["NewName"].Value;
        }

        return line;
    }


    public void DiffTool(Decision file)
    {
        if (file.DecisionType == DecisionType.ReviewNewFile)
        {
            _gitCaller.NewFileDiff(file.FileStatus.File);
        }
        else
        {
            _gitCaller.DiffTool(file.FileStatus.File);
        }
    }

    public IEnumerable<UserFeedback> GetActions(DecisionType decisionDecisionType) =>
        decisionDecisionType switch
        {
            DecisionType.ReviewChanges => new[]
            {
                UserFeedback.DiscardChanges,
                UserFeedback.Stage,
                UserFeedback.Relaunch,
                UserFeedback.Ignore,
            },
            DecisionType.ReviewNewFile => new[]
            {
                UserFeedback.DeleteFile,
                UserFeedback.Stage,
                UserFeedback.Relaunch,
                UserFeedback.Ignore,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(decisionDecisionType), decisionDecisionType, null),
        };

    public void PerformAction(UserFeedback userFeedback, FileStatus file) =>
        ((Action<string>)(userFeedback switch
        {
            UserFeedback.Stage => _gitCaller.Add,
            UserFeedback.DiscardChanges => _gitCaller.Discard,
            UserFeedback.DeleteFile => _gitCaller.Delete,
            UserFeedback.Relaunch => Noop,
            UserFeedback.Ignore => Ignore,
            _ => throw new ArgumentOutOfRangeException(nameof(userFeedback), userFeedback, null),
        }))(file.File);

    private void Ignore(string file) => _ignoreList.Add(file);

    public bool NeedConfirmation(UserFeedback userFeedback) =>
        userFeedback switch
        {
            UserFeedback.DiscardChanges => true,
            UserFeedback.DeleteFile => true,
            _ => false,
        };

    private void Noop(string _)
    {
    }
}