using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ashampoo
{
    public abstract class IFileSystemScanner
    {
        protected readonly CancellationTokenSource CancellationToken = new();

        protected ScanOptions? ScanOptions;

        protected bool IsInProgress = false;

        private readonly object _pauseLock = new();
        private bool _paused = false;

        public bool Paused
        {
            get
            {
                lock (_pauseLock)
                {
                    return _paused;
                }
            }
            set
            {
                lock (_pauseLock)
                {
                    _paused = value;
                }
            }
        }

        public abstract IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions);

        public void Cancel()
        {
            CancellationToken.Cancel();
        }

        public bool IsRunning()
        {
            return IsInProgress;
        }

        protected ScanResult MakeResult(DirectoryInfo dirInfo)
        {
            var fileCount = dirInfo.GetFiles().Length;
            var size = dirInfo.GetFiles().Sum(f => f.Length) / FileSize.Megabyte;

            ScanResult result = new ScanResult
            {
                DirPath = dirInfo.FullName,
                FileCount = fileCount,
                Size = size
            };

            return result;
        }
    }

    public class ScanResult
    {
        public string DirPath { get; set; } = string.Empty;
        public int FileCount { get; set; } = 0;
        public long Size { get; set; } = 0;
    }

    public class ScanOptions
    {
        public string RootDir { get; set; } = string.Empty;
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public CompositeFilter CompositeFilter = new();

        public ScanOptions(string rootDir)
        {
            RootDir = rootDir;

            CompositeFilter.AddFilter(new SizeFilter(10 * FileSize.Megabyte));
        }
    }

    public enum ScannerType
    {
        Simple,
        Quick
    }

    public static class FilesystemScannerFactory
    {
        public static IFileSystemScanner CreateScanner(ScannerType type)
        {
            switch (type)
            {
                case ScannerType.Simple:
                    return new SimpleFileSystemScanner();
                case ScannerType.Quick:
                    return new QuickFileSystemScanner();
                default:
                    throw new ArgumentException("Invalid scanner type");
            }
        }
    }
}
