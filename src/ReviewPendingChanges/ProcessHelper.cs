using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ReviewPendingChanges;

public static class ProcessHelper
{
    public static string[] StartAndWait(string fileName, string workingDirectory, params string[] arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };

        var sb = new List<string>();
        process.OutputDataReceived += delegate (object _, DataReceivedEventArgs e) { sb.Add(e.Data); };
        process.ErrorDataReceived += delegate (object _, DataReceivedEventArgs e) { sb.Add(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return sb.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }
}