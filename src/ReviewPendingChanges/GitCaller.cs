using System;
using System.IO;
using System.Linq;
using System.Text;
using RunProcess;

namespace ReviewPendingChanges
{
    public class GitCaller : IGitCaller
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromHours(1);
        private static readonly Encoding _defaultEncoding = Encoding.UTF8;

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

        private void OpenTextEditor(string arguments)
        {
            Logger.Verbose($"code {arguments}");
            using var proc = new ProcessHost(@"C:\Program Files\Microsoft VS Code\Code.exe", _repository);
            proc.Start(arguments);
            proc.WaitForExit(_defaultTimeout);
        }

        private string[] SimpleGitCommand(string arguments)
        {
            Logger.Verbose($"git {arguments}");
            using var proc = new ProcessHost("git.exe", _repository);
            proc.Start(arguments);
            proc.WaitForExit(_defaultTimeout);
            return proc.StdOut.ReadAllText(_defaultEncoding)
                .Split('\n')
                .Where(l => !string.IsNullOrEmpty(l))
                .ToArray();
        }
    }
}
