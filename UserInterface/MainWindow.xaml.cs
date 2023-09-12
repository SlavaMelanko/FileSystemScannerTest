using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ashampoo;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Ashampoo.IFileSystemScanner Scanner;

        public MainWindow()
        {
            InitializeComponent();

            Scanner = FilesystemScannerFactory.CreateScanner(ScannerType.Quick);

            VolumeComboBox.ItemsSource = Ashampoo.VolumeProvider.GetVolumeList();
            VolumeComboBox.SelectedIndex = 0;
        }

        private async void StartScanning(object sender, RoutedEventArgs e)
        {
            if (Scanner.IsScanning())
            {
                TogglePause();
            }
            else
            {
                UpdateBeforeScanning();

                await Task.Run(async () =>
                {
                    var options = new ScanOptions
                    {
                        RootDir = GetSelectedVolume()
                    };

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    await foreach (var result in Scanner.ScanAsync(options))
                    {
                        Log($"{result.DirPath}: {result.FileCount} files / {result.Size} MB\r");
                    }

                    stopwatch.Stop();
                    Log($"\rElapsed Time: {stopwatch.Elapsed}\r");
                });

                UpdateAfterScanning();
            }
        }

        private string GetSelectedVolume()
        {
            return Dispatcher.Invoke(() => VolumeComboBox.SelectedValue as string) ?? string.Empty;
        }

        private void Log(string entry)
        {
            Dispatcher.Invoke(() =>
            {
                TextBox.AppendText(entry);
                TextBox.ScrollToEnd();
            });
        }

        private void TogglePause()
        {
            Scanner.Paused = (ScanButton.Content as string == "Pause");
            ScanButton.Content = Scanner.Paused ? "Resume" : "Pause";
        }

        private void UpdateBeforeScanning()
        {
            ScanButton.Content = "Pause";
            TextBox.Document = new FlowDocument();
            VolumeComboBox.IsEnabled = false;
        }

        private void UpdateAfterScanning()
        {
            ScanButton.Content = "Scan";
            VolumeComboBox.IsEnabled = true;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs args)
        {
            Scanner.Cancel();
        }
    }
}
