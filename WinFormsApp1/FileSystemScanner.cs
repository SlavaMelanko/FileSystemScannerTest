namespace Ashampoo
{
    public class FileSystemScanner
    {
        /// <summary>
        /// Used to pause, resume, and cancel the scan process.
        /// </summary>
        private readonly CancellationTokenSource _CancellationTokenSource;

        /// <summary>
        /// Used to filter out duplications on output.
        /// </summary>
        private readonly HashSet<string> _DirectorySet;

        public FileSystemScanner()
        {
            _CancellationTokenSource = new CancellationTokenSource();
            _DirectorySet = new HashSet<string>();
        }

        public bool Paused { get; set; } = false;

        public void Cancel()
        {
            _CancellationTokenSource.Cancel();
        }

        public async IAsyncEnumerable<DirectoryInfo> ScanAsync(string path)
        {
            var directoriesToScan = new Stack<DirectoryInfo>();
            directoriesToScan.Push(new DirectoryInfo(path));

            _DirectorySet.Clear();

            while (directoriesToScan.Count > 0)
            {
                var directory = directoriesToScan.Pop();

                if (!IsDirectoryCanBeChecked(directory))
                {
                    continue;
                }

                foreach (var file in directory.GetFiles().Where(f => f.Length > FileSize.TenMegabytes))
                {
                    if (!_DirectorySet.Contains(directory.FullName)) // filters out duplications
                    {
                        _DirectorySet.Add(directory.FullName);
                        yield return directory;
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
            return _CancellationTokenSource.Token.IsCancellationRequested;
        }
    }
}
