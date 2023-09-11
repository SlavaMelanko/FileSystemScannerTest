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
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (IsScanStarted)
            {
                TogglePause();
            }
            else
            {
                string path = GetVolume();

                button1.Text = "Pause";

                IsScanStarted = true;

                richTextBox1.Clear();

                await Task.Run(async () =>
                {
                    await foreach (var directory in Scanner.ScanAsync(path))
                    {
                        var totalFilesWithoutSubdirs = directory.GetFiles().Length;
                        var totalSizeWithoutSubdirs = directory.GetFiles().Sum(f => f.Length) / FileSize.Megabyte;
                        this.Invoke(() =>
                        {
                            richTextBox1.AppendText($"{directory.FullName}: {totalFilesWithoutSubdirs} files / {totalSizeWithoutSubdirs} MB\n");
                            richTextBox1.ScrollToCaret();
                        });
                    }
                });

                IsScanStarted = false;

                button1.Text = "Scan";
            }
        }

        private string GetVolume()
        {
            return comboBox1.SelectedItem.ToString() ?? "C:\\"; // remove warning CS8600
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
    }
}