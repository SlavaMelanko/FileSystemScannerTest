using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ashampoo
{
    [Obsolete("This class is deprecated and should not be used. Consider using the new QuickFileSystemScanner instead.")]
    public class SimpleFileSystemScanner : FileSystemScanner
    {
        /// <summary>
        /// Used to filter out duplications on output.
        /// </summary>
        private readonly HashSet<string> _DirectorySet;

        public SimpleFileSystemScanner()
        {
            _DirectorySet = new HashSet<string>();
        }

        public override async IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions)
        {
            ScanOptions = scanOptions;

            var directoriesToScan = new Stack<DirectoryInfo>();
            directoriesToScan.Push(new DirectoryInfo(ScanOptions.RootDir));

            _DirectorySet.Clear();

            while (directoriesToScan.Count > 0)
            {
                var directory = directoriesToScan.Pop();

                if (!IsDirectoryCanBeChecked(directory))
                {
                    continue;
                }

                foreach (var file in directory.GetFiles().Where(f => f.Length > ScanOptions.MinFileSize))
                {
                    if (!_DirectorySet.Contains(directory.FullName)) // filters out duplications
                    {
                        _DirectorySet.Add(directory.FullName);
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

            _DirectorySet.Clear();
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
