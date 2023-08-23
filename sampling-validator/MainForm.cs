using FluentFTP;
using sampling_validator.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace sampling_validator
{
    public partial class mainform : Form
    {
        private FtpClient ftpClient;

        public mainform()
        {
            InitializeComponent();

            ftpClient = new FtpClient("senunas.ipdisk.co.kr", 2348)
            {
                Encoding = Encoding.UTF8,
                Credentials = new System.Net.NetworkCredential("id", "pw")
            };
            ftpClient.Connect();
            FtpListItem[] listItems = ftpClient.GetListing("/HDD1/pak_hyun/");
            foreach (var item in listItems)
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = item.Name;
                listViewItem.SubItems.Add(item.Modified.ToLocalTime().ToString());
                listView.Items.Add(listViewItem);
            }
        }

        private List<int> diff(List<long> numbers)
        {
            var diff = new List<int>();
            for (int i = 0; i < numbers.Count - 1; i++)
                diff.Add((int)(numbers[i + 1] - numbers[i]));
            return diff;
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;
            string filename = listView.SelectedItems[0].Text;
            Stream outStream = new MemoryStream();
            ftpClient.DownloadStream(outStream, "/HDD1/pak_hyun/" + filename);
            string text;
            using (var reader = new StreamReader(outStream, Encoding.UTF8))
            {
                outStream.Position = 0;
                text = reader.ReadToEnd().Replace("\n", Environment.NewLine);
            }
            textBox1.Text = text;
            if (filename.Contains("heartrate"))
            {
                try
                {
                    List<long> timestamps = text.Split(new string[] { "\n" }, StringSplitOptions.None).ToList()
                         .ConvertAll(x => x.Split(new string[] { "," }, StringSplitOptions.None)[0])
                         .Skip(1)
                         .Where(x => !string.IsNullOrEmpty(x))
                         .ToList()
                         .ConvertAll(long.Parse);
                    List<int> dts = diff(timestamps);
                    int expectedNumbers = (int)((timestamps.Last() - timestamps.First()) / 1000);
                    textBox2.Text = Resources.Template.Replace("\n", Environment.NewLine)
                        .Replace("%expectedNumbers%", expectedNumbers.ToString())
                        .Replace("%actualNumbers%", timestamps.Count.ToString())
                        .Replace("%missingNumbers%", (expectedNumbers - timestamps.Count).ToString())
                        .Replace("%overNumbers%", dts.Where(x => x > 1100).Count().ToString())
                        .Replace("%over10Numbers%", dts.Where(x => x > 10000).Count().ToString());
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                textBox2.Text = "";
            }
        }
    }
}
