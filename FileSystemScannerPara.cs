using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ashampoo
{
    public class FileSystemScannerPara
    {
        private readonly CancellationTokenSource _CancellationToken;
        private readonly ConcurrentBag<string> _FoundDirs;

        public FileSystemScannerPara()
        {
            _CancellationToken = new CancellationTokenSource();
            _FoundDirs = new ConcurrentBag<string>();
        }

        public bool Paused { get; set; } = false;

        public void Cancel()
        {
            _CancellationToken.Cancel();
        }

        public async Task ScanAsync(string rootDirectory)
        {
            _FoundDirs.Clear();

            await Task.Run(() =>
            {
                Parallel.ForEach(Directory.EnumerateFiles(rootDirectory, "*", GetEnumerationOptions()), GetParallelOptions(), file =>
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Length > FileSize.TenMegabytes)
                        {
                            var dirName = fileInfo.DirectoryName;
                            if (dirName != null && !_FoundDirs.Contains(dirName))
                            {
                                _FoundDirs.Add(dirName);
                                //Console.WriteLine($"{dirName}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message}");
                    }
                });
            });

            Console.WriteLine($"{_FoundDirs.Count}");
        }

        private EnumerationOptions GetEnumerationOptions()
        {
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
            };

            return enumerationOptions;
        }

        private ParallelOptions GetParallelOptions()
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = _CancellationToken.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            return parallelOptions;
        }
    }
}
