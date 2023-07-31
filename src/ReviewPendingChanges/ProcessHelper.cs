using System;
using System.Diagnostics;
using System.Text;

namespace ReviewPendingChanges;

public static class ProcessHelper
{
    public static string StartAndWait(string fileName, string workingDirectory, string arguments)
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
            }
        };

        process.Start();
        process.WaitForExit();
        return process.StandardOutput.ReadToEnd();
    }
}