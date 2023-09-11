using Ashampoo;

using System.IO;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class MainWindow : Form
    {
        private Ashampoo.FileSystemScanner Scanner;
        private bool IsScanStarted;

        public MainWindow()
        {
            InitializeComponent();

            Scanner = new Ashampoo.FileSystemScanner();

            comboBox1.Items.AddRange(Ashampoo.VolumeProvider.GetVolumeList().ToArray());
            comboBox1.SelectedIndex = 0;

            IsScanStarted = false;

            this.FormClosing += MainWindow_FormClosing;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (IsScanStarted)
            {
                TogglePause();
            }
            else
            {
                UpdateBeforeScanning();

                IsScanStarted = true;

                try
                {
                    await Task.Run(async () =>
                    {
                        await foreach (var directory in Scanner.ScanAsync(GetVolume()))
                        {
                            if (Scanner.IsCancellationRequested())
                            {
                                break;
                            }

                            var totalFilesWithoutSubdirs = directory.GetFiles().Length;
                            var totalSizeWithoutSubdirs = directory.GetFiles().Sum(f => f.Length) / FileSize.Megabyte;
                            this.Invoke(() =>
                            {
                                richTextBox1.AppendText($"{directory.FullName}: {totalFilesWithoutSubdirs} files / {totalSizeWithoutSubdirs} MB\n");
                                richTextBox1.ScrollToCaret();
                            });
                        }
                    });
                }
                catch (Exception)
                {
                    ; // TODO: Must be a better solution
                }

                IsScanStarted = false;

                UpdateAfterScanning();
            }
        }

        private void UpdateBeforeScanning()
        {
            button1.Text = "Pause";
            richTextBox1.Clear();
            comboBox1.Enabled = false;
        }

        private void UpdateAfterScanning()
        {
            button1.Text = "Scan";
            comboBox1.Enabled = true;
        }

        private string GetVolume()
        {
            string volumeLetter = string.Empty;
            this.Invoke(() =>
            {
                volumeLetter = comboBox1.SelectedItem.ToString() ?? "C:\\"; // remove warning CS8600
            });
            return volumeLetter;
        }

        private void TogglePause()
        {
            if (button1.Text == "Pause")
            {
                Scanner.Paused = true;
                button1.Text = "Resume";
            }
            else
            {
                Scanner.Paused = false;
                button1.Text = "Pause";
            }
        }

        private void MainWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!Scanner.Paused)
                {
                    Scanner.Cancel();
                }
            }
        }
    }
}