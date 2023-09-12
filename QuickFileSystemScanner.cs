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
    public class QuickFileSystemScanner : FileSystemScanner
    {
        private readonly ConcurrentQueue<ScanResult> _Result;

        /// <summary>
        /// Additional collection that just removes duplicates.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _UniqueDirNames;

        public QuickFileSystemScanner()
        {
            _Result = new ConcurrentQueue<ScanResult>();
            _UniqueDirNames = new ConcurrentDictionary<string, bool>();
        }

        public override async IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions)
        {
            ScanOptions = scanOptions;

            _Result.Clear();
            _UniqueDirNames.Clear();

            var enumerateOptions = GetEnumerateOptions();
            var parallelOptions = GetParallelOptions();

            bool completed = false;

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
                            if (dirName is not null && !_UniqueDirNames.ContainsKey(dirName))
                            {
                                if (_UniqueDirNames.TryAdd(dirName, true))
                                {
                                    DirectoryInfo dirInfo = new DirectoryInfo(dirName);
                                    _Result.Enqueue(MakeResult(dirInfo));
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
                if (_Result.TryDequeue(out var result))
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
