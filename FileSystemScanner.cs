using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashampoo
{
    public abstract class FileSystemScanner
    {
        protected readonly CancellationTokenSource CancellationToken;
        protected ScanOptions? ScanOptions;
        public bool Paused { get; set; } = false;

        public FileSystemScanner()
        {
            CancellationToken = new CancellationTokenSource();
        }

        public abstract IAsyncEnumerable<ScanResult> ScanAsync(ScanOptions scanOptions);

        public abstract void Cancel();

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
        public long MinFileSize { get; set; } = 10 * FileSize.Megabyte;
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
    }

    public enum ScannerType
    {
        Simple,
        Quick
    }

    public static class FilesystemScannerFactory
    {
        public static FileSystemScanner CreateScanner(ScannerType type)
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
