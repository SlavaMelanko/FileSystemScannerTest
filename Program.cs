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
        var scanner = FilesystemScannerFactory.CreateScanner(ScannerType.Quick);

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

        var options = new ScanOptions
        {
            RootDir = "C:\\"
        };

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        await foreach (var result in scanner.ScanAsync(options))
        {
            Log.Info($"{result.DirPath}: {result.FileCount} / {result.Size} MB");
        }

        stopwatch.Stop();
        Log.Info($"\nElapsed Time: {stopwatch.Elapsed}");
    }
}
