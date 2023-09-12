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
    public class ScanResult
    {
        public string DirPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; } = 0;
        public long DirSize { get; set; } = 0;
    }

    public class QuickFileSystemScanner
    {
        private readonly CancellationTokenSource _CancellationToken;
        private readonly ConcurrentQueue<DirectoryInfo> _FoundDirs;
        private readonly ConcurrentDictionary<string, bool> _UniqueDirs;

        public bool Paused { get; set; } = false;

        public QuickFileSystemScanner()
        {
            _CancellationToken = new CancellationTokenSource();
            _FoundDirs = new ConcurrentQueue<DirectoryInfo>();
            _UniqueDirs = new ConcurrentDictionary<string, bool>();
        }

        public async IAsyncEnumerable<DirectoryInfo> ScanAsync(string volume)
        {
            _FoundDirs.Clear();
            _UniqueDirs.Clear();

            var enumerateOptions = GetEnumerateOptions();
            var parallelOptions = GetParallelOptions();

            bool completed = false;

            Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(Directory.EnumerateFiles(volume, "*", enumerateOptions), parallelOptions, file =>
                    {
                        while (Paused)
                        {
                            Thread.Sleep(100); // actual pause
                            // TODO: It ain't stupid if it works, but is it a good idea to use sleep here?

                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                            {
                                return; // cancel on pause
                            }
                        }

                        if (parallelOptions.CancellationToken.IsCancellationRequested)
                        {
                            return; // cancel on running
                        }

                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length > FileSize.TenMegabytes)
                        {
                            var dirName = fileInfo.DirectoryName;
                            if (dirName != null && !_UniqueDirs.ContainsKey(dirName))
                            {
                                if (_UniqueDirs.TryAdd(dirName, true))
                                {
                                    DirectoryInfo dirInfo = new DirectoryInfo(dirName);
                                    _FoundDirs.Enqueue(dirInfo);
                                }
                            }
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    ; // iteration cancelled
                }
                finally
                {
                    completed = true;
                }
            });

            while (!completed)
            {
                if (_FoundDirs.TryDequeue(out var dirInfo))
                {
                    yield return dirInfo;
                }
            }
        }

        public void Cancel()
        {
            _CancellationToken.Cancel();
        }

        private EnumerationOptions GetEnumerateOptions()
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
