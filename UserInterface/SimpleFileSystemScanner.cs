using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ashampoo
{
    [Obsolete("This class is deprecated and should not be used. Consider using the new QuickFileSystemScanner instead.")]
    public class SimpleFileSystemScanner : IFileSystemScanner
    {
        /// <summary>
        /// Used to filter out duplications on output.
        /// </summary>
        private readonly HashSet<string> _directorySet = new();

        public override async IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions)
        {
            ScanOptions = scanOptions;

            var directoriesToScan = new Stack<DirectoryInfo>();
            directoriesToScan.Push(new DirectoryInfo(ScanOptions.RootDir));

            IsRunning = true;
            _directorySet.Clear();

            while (directoriesToScan.Count > 0)
            {
                var directory = directoriesToScan.Pop();

                if (!IsDirectoryCanBeChecked(directory))
                {
                    continue;
                }

                foreach (var file in directory.GetFiles().Where(f => f.Length > ScanOptions.MinFileSize))
                {
                    if (!_directorySet.Contains(directory.FullName)) // filters out duplications
                    {
                        _directorySet.Add(directory.FullName);
                        yield return MakeResult(directory);
                    }
                }

                foreach (var subdirectory in directory.GetDirectories())
                {
                    while (Paused)
                    {
                        await Task.Delay(100); // actual pause

                        if (IsCancellationRequested())
                        {
                            yield break; // cancel on pause
                        }
                    }

                    if (IsCancellationRequested())
                    {
                        yield break; // cancel on running
                    }

                    if (IsDirectoryCanBeChecked(subdirectory))
                    {
                        directoriesToScan.Push(subdirectory);
                    }
                }
            }

            IsRunning = false;
            _directorySet.Clear();
        }

        public override void Cancel()
        {
            CancellationToken.Cancel();
        }

        private bool IsDirectoryCanBeChecked(DirectoryInfo directory)
        {
            try
            {
                _ = directory.GetFiles();
                _ = directory.Attributes;

                return true;
            }
            catch (NullReferenceException)
            {
                // Is it a file?
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // No read permissions.
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsCancellationRequested()
        {
            return CancellationToken.Token.IsCancellationRequested;
        }
    }
}
