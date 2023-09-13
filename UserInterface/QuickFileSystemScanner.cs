﻿using System;
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
        private readonly ConcurrentQueue<ScanResult> _result = new();

        /// <summary>
        /// Additional collection that just removes duplicates.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _uniqueDirNames = new();

        public override async IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions)
        {
            ScanOptions = scanOptions;

            _result.Clear();
            _uniqueDirNames.Clear();

            var enumerateOptions = GetEnumerateOptions();
            var parallelOptions = GetParallelOptions();

            IsInProgress = true;

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
                        if (ScanOptions.CompositeFilter.Matches(fileInfo))
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
                    IsInProgress = false;
                }
            });

            while (IsInProgress)
            {
                if (_result.TryDequeue(out var result))
                {
                    yield return result;
                }
            }
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
