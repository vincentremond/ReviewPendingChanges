using System;
using System.Collections.Generic;
using System.Linq;
using ReviewPendingChanges.Records;

namespace ReviewPendingChanges
{
    internal static class Program
    {
        private static void Main(params string[] args)
        {
            var repository = args.Any()
                ? args.First()
                : Environment.CurrentDirectory;

            var helper = new GitHelper(new GitCaller(repository));

            while (true)
            {
                var groups = helper.GetFilesStatus()
                    .Select(DecisionMatrix.WhatToDo)
                    .GroupBy(
                        i => i.DecisionType switch
                        {
                            DecisionType.Undefined => DecisionTypeGroup.Error,
                            DecisionType.None => DecisionTypeGroup.Ignore,
                            _ => DecisionTypeGroup.Operate,
                        }
                    ).ToDictionary(
                        g => g.Key,
                        g => g.ToList().AsReadOnly()
                    );

                if (groups.TryGetValue(DecisionTypeGroup.Error, out var errors))
                {
                    Logger.Error("Error − Could not define what to do for this :", ObjectDumper.Dump(errors));
                    break;
                }

                if (groups.TryGetValue(DecisionTypeGroup.Operate, out var toOperate))
                {
                    var decision = toOperate.First();
                    helper.DiffTool(decision);
                    var actions = helper.GetActions(decision.DecisionType);
                    var userFeedback = AskUser(decision.FileStatus.File, actions);
                    if (ConfirmIfNeeded(helper, userFeedback, decision.FileStatus.File))
                    {
                        Logger.Write("", $"{userFeedback} for {decision.FileStatus.File}");
                        helper.PerformAction(userFeedback, decision.FileStatus);
                    }
                }
                else
                {
                    Logger.Write("Nothing to do");
                    break;
                }
            }
        }

        private static bool ConfirmIfNeeded(GitHelper helper, UserFeedback userFeedback, string file)
        {
            if (helper.NeedConfirmation(userFeedback))
            {
                return ConfirmAction($"Are you sure you want to {userFeedback} this file ? [y/n]\n    '{file}'");
            }

            return true;
        }

        private static bool ConfirmAction(string actionToConfirm)
        {
            while (true)
            {
                Logger.Write(actionToConfirm);
                var key = Logger.ReadKey();
                switch (key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                }
            }
        }

        private static UserFeedback AskUser(string file, IEnumerable<UserFeedback> actions)
        {
            void ShowOptions(List<(ConsoleKey Key, string Message, UserFeedback Action)> list)
            {
                var lines =
                    new[]
                        {
                            $"File: {file}",
                            "Possible actions :",
                        }.Union(list.Select(l => $" - [{l.Key}] {l.Message}"))
                        .ToArray();
                Logger.Write(lines);
            }

            bool ReadResponse(List<(ConsoleKey Key, string Message, UserFeedback Action)> valueTuples, out UserFeedback userFeedback)
            {
                var key = Logger.ReadKey();
                if (valueTuples.TryGetValue(i => i.Key == key, out var result))
                {
                    userFeedback = result.Action;
                    return true;
                }

                userFeedback = default;
                return false;
            }

            static (ConsoleKey Key, string Message, UserFeedback Action) GetKeyAndMessageForAction(UserFeedback action)
            {
                return action switch
                {
                    UserFeedback.Stage => (ConsoleKey.S, "Stage changes", action),
                    UserFeedback.DiscardChanges => (ConsoleKey.D, "Discard changes", action),
                    UserFeedback.Relaunch => (ConsoleKey.R, "Relaunch tool", action),
                    _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
                };
            }

            var validKeys = actions.Select(GetKeyAndMessageForAction).ToList();

            while (true)
            {
                ShowOptions(validKeys);
                if (ReadResponse(validKeys, out var userFeedback))
                {
                    return userFeedback;
                }
            }
        }
    }
}
