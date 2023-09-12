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
    public class QuickFileSystemScanner : IFileSystemScanner
    {
        private readonly ConcurrentQueue<ScanResult> _result;

        /// <summary>
        /// Additional collection that just removes duplicates.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _uniqueDirNames;

        public QuickFileSystemScanner()
        {
            _result = new ConcurrentQueue<ScanResult>();
            _uniqueDirNames = new ConcurrentDictionary<string, bool>();
        }

        public override async IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions)
        {
            ScanOptions = scanOptions;

            _result.Clear();
            _uniqueDirNames.Clear();

            var enumerateOptions = GetEnumerateOptions();
            var parallelOptions = GetParallelOptions();

            IsRunning = true;

            Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(Directory.EnumerateFiles(ScanOptions.RootDir, "*", enumerateOptions), parallelOptions, file =>
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
                        if (fileInfo.Length > ScanOptions.MinFileSize)
                        {
                            var dirName = fileInfo.DirectoryName;
                            if (dirName is not null && !_uniqueDirNames.ContainsKey(dirName))
                            {
                                if (_uniqueDirNames.TryAdd(dirName, true))
                                {
                                    DirectoryInfo dirInfo = new DirectoryInfo(dirName);
                                    _result.Enqueue(MakeResult(dirInfo));
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
                    IsRunning = false;
                }
            });

            while (IsRunning)
            {
                if (_result.TryDequeue(out var result))
                {
                    yield return result;
                }
            }
        }

        public override void Cancel()
        {
            CancellationToken.Cancel();
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
                CancellationToken = CancellationToken.Token,
                MaxDegreeOfParallelism = ScanOptions?.ProcessorCount ?? 1
            };

            return parallelOptions;
        }
    }
}
