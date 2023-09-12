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
        var scanner = new FileSystemScannerPara();

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

        string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        await scanner.ScanAsync("C:\\");

        /*await foreach (var directory in scanner.ScanAsync("C:\\"))
        {
            //var totalFilesWithoutSubdirs = directory.GetFiles().Length;
            //var totalSizeWithoutSubdirs = directory.GetFiles().Sum(f => f.Length) / FileSize.Megabyte;
            Log.Info($"{directory.FullName}");
        }*/

        stopwatch.Stop();
        Log.Info($"Elapsed Time: {stopwatch.Elapsed}");
    }
}
