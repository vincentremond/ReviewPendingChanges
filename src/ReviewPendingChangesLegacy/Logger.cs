using System;
using System.Drawing;
using Console = Colorful.Console;

namespace ReviewPendingChangesLegacy;

public class Logger
{
    public static void Error(params string[] logs) => logs.ForEach(e => Console.WriteLine(e, Color.Red));
    public static void Verbose(params string[] logs) => logs.ForEach(e => Console.WriteLine(e, Color.Gray));
    public static void Write(params string[] logs) => logs.ForEach(Console.WriteLine);
    public static ConsoleKey ReadKey() => Console.ReadKey().Key;
}