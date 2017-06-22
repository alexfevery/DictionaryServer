using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;


namespace DictionaryServer
{
    public partial class Display : Form
    {
        public System.Windows.Forms.Timer UpdateTimer = new System.Windows.Forms.Timer();
        public Display()
        {
            InitializeComponent();
            this.Text = "Dictionary Server";
            Display1.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width, ClientRectangle.Height * 1 / 3);
            Display2.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y + Display1.Height, this.ClientRectangle.Width, this.ClientRectangle.Height - Display1.Height);

            UpdateTimer.Interval = 1000;
            UpdateTimer.Tick += new EventHandler(UpdateDisplay);
            UpdateTimer.Tag = 0;
            UpdateTimer.Start();
            ThreadPool.QueueUserWorkItem(delegate
            {
                Program.Init();
            }, null);
            ThreadPool.QueueUserWorkItem(delegate
            {
                Program.WaitForConnections();
            }, null);
            Display1.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width, Display1.Font.Height * 7);
            Display2.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y + Display1.Height, this.ClientRectangle.Width, this.ClientRectangle.Height - Display1.Height);
        }

        private void UpdateDisplay(object sender, EventArgs e)
        {
            UpdateTimer.Tag = int.Parse(UpdateTimer.Tag.ToString()) + 1;
            try
            {
                if (!Program.Loading)
                {
                    if (!File.Exists(Program.userlog) || (!File.ReadAllText(Program.userlog).Contains(DateTime.Now.ToLongDateString()) && DateTime.Now.Hour == 7))
                    {
                        Program.Init();
                        File.AppendAllText(Program.userlog, DateTime.Now.ToLongDateString() + Environment.NewLine + Environment.NewLine);
                        Program.UserList.Clear();
                        Program.threadlist.Clear();
                    }
                    if (int.Parse(UpdateTimer.Tag.ToString()) >= 100)
                    {
                        UpdateTimer.Tag = 0;
                        foreach (string item in Program.RecentIPList)
                        {
                            if (!Program.BanList.Contains(item) && Program.RecentIPList.Count(x => x == item) >= 100)
                            {
                                Program.BanList.Add(item);
                                //Credentials stored separately on a local machine for obvious reasons.
                                string email = File.ReadAllText(@"C:\Users\alexf\Google Drive\Computer\Documents\emailcredentials.txt").Split('\t')[0];
                                string password = File.ReadAllText(@"C:\Users\alexf\Google Drive\Computer\Documents\emailcredentials.txt").Split('\t')[1];

                                SmtpClient client = new SmtpClient("smtp.gmail.com", 587) { Credentials = new NetworkCredential(email, password), EnableSsl = true };
                                MailMessage message = new MailMessage(email, email);
                                message.Subject = "Scraping Attempt Detected";
                                message.Body = "IP: " + item + Environment.NewLine + "Blocked";
                                client.SendAsync(message, null);
                            }
                        }
                        Program.RecentIPList.Clear();
                    }


                    Display1.Clear();
                    Display1.AppendText("Server Version: " + Program.version + Environment.NewLine);
                    Display1.AppendText("Server URL: " + "http://" + Program.OwnIPAddress + ":" + Program.Port + "/" + Environment.NewLine);
                    TimeSpan uptime = DateTime.Now.Subtract(Program.starttime);
                    Display1.AppendText("Server Uptime: " + (uptime.Days == 1 ? uptime.Days + " day " : (uptime.Days > 1 ? uptime.Days + " days " : "")) + (uptime.Hours == 1 ? uptime.Hours + " hour " : (uptime.Hours > 1 ? uptime.Hours + " hours " : "")) + (uptime.Minutes == 1 ? uptime.Minutes + " minute " : (uptime.Minutes > 1 ? uptime.Minutes + " minutes " : "")) + (uptime.Seconds == 1 ? uptime.Seconds + " second " : (uptime.Seconds > 1 ? uptime.Seconds + " seconds " : "")) + Environment.NewLine);
                    Display1.AppendText("Total Connections: " + Program.totalconnections + Environment.NewLine);
                    Display1.AppendText("Unique Connections: " + Program.uniqueconnections + Environment.NewLine);
                    Display1.AppendText(("Last Request Processing Time: " + Program.timer.ElapsedMilliseconds) + Environment.NewLine);
                    Display1.AppendText("Waiting for connections...");
                    foreach (string line in Program.UserList.ToList())
                    {
                        if (Display2.Lines.Contains(line)) { continue; }
                        Display2.SelectionStart = 0;
                        Display2.SelectionLength = 0;
                        Display2.SelectedText = (line + '\n');
                        Display2.Update();
                    }
                }
            }
            catch { }
        }

        private void Display_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized) { this.Visible = false; }
            Display1.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width, Display1.Font.Height * 7);
            Display2.SetBounds(this.ClientRectangle.X, this.ClientRectangle.Y + Display1.Height, this.ClientRectangle.Width, this.ClientRectangle.Height - Display1.Height);
        }

        private void Display_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(1);
        }
    }
}
