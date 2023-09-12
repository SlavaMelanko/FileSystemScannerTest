using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

using Ashampoo;

class App
{
    static async Task Main(string[] args)
    {
        var scanner = new QuickFileSystemScanner();

        _ = Task.Run(() =>
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.P)
                {
                    scanner.Paused = true;
                    Log.Info("Paused. Press 'R' to resume.");
                }
                else if (key == ConsoleKey.R)
                {
                    scanner.Paused = false;
                    Log.Info("Resumed.");
                }
                else if (key == ConsoleKey.C)
                {
                    scanner.Cancel();
                    Log.Info("Cancelled.");
                    break;
                }
            }
        });

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        await foreach (var dirInfo in scanner.ScanAsync("C:\\"))
        {
            var totalFiles = dirInfo.GetFiles().Length;
            var totalSize = dirInfo.GetFiles().Sum(f => f.Length) / FileSize.Megabyte;
            Log.Info($"{dirInfo.FullName}: {totalFiles} / {totalSize} MB");
        }

        stopwatch.Stop();
        Log.Info($"Elapsed Time: {stopwatch.Elapsed}");
    }
}
