using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace DictionaryServer
{
    public class Program
    {
        public static string serverpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dictionary-server - Copy");
        public static HttpListener listener = new HttpListener();
        static Thread SpawnNewClient;
        public static List<string> UserList = new List<string>();
        public static List<string> IPList = new List<string>();
        public static List<string> BanList = new List<string>();
        public static List<string> CurrentIPList = new List<string>();
        public static List<Thread> threadlist = new List<Thread>();

        public static Dictionary<string[], DictionaryEntry> DictionaryDataPinyin;
        public static Dictionary<string[], DictionaryEntry> DictionaryExactWord;
        public static Dictionary<string, DictionaryEntry> DictionaryContainingWord;
        public static Dictionary<string, SentenceEntry> SentenceContainingWord;
        public static List<SentenceEntry> SentenceData;
        public static List<DictionaryEntry> DictionaryData;
        public static bool DataAccessInProgress;

        public static List<DictionaryEntry> DictionaryDatatemp;
        public static List<SentenceEntry> SentenceDatatemp;

        public static string log = Path.Combine(serverpath, "log.txt");
        public static string userlog = Path.Combine(serverpath, "userlog.txt");
        public static string OwnIPAddress = new WebClient().DownloadString(@"http://canihazip.com/s").Trim();

        public static int newentries = -1;
        public static int dictionary = -1;
        public static int sentence = -1;
        public static int newsentences = -1;
        public static int writecount = -1;


        public static NotifyIcon trayIcon = new NotifyIcon();
        public static int Port = 8002;
        public static Display display1 = new Display();
        public static DateTime starttime = DateTime.Now;
        public static int totalconnections = 0;
        public static int uniqueconnections = 0;
        public static string version = "0.01";
        public static Stopwatch timer = new Stopwatch();
        public static bool Loading = true;
        public static bool FullReset = false;
        public static Random random = new Random();
        public static DictionaryEntry lastsearch = new DictionaryEntry { chinese = "殺人狂", mainland = "杀人狂", alternate = "", zhuyin = "ㄕㄚ ㄖㄣˊ ㄎㄨㄤˊ", pinyin = "shā rén kuáng", definition = new List<string> { "homicidal maniac" } };

        [STAThread]
        static void Main()
        {
            //copy "$(TargetDir)$(TargetFileName)" "C:\Users\alexf\Google Drive\Computer\Desktop\dictionary-server\$(ProjectName).exe"
            Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName.Split('.')[0]).Where(x => x.Id != Process.GetCurrentProcess().Id).ToList().ForEach(x => x.Kill());
            Application.Run(display1);
        }

        public static void Init()
        {
            Loading = true;
            if (!File.Exists(Path.Combine(Program.serverpath, "newentries.txt"))) { File.Create(Path.Combine(Program.serverpath, "newentries.txt")).Close(); }

            ThreadPool.QueueUserWorkItem(delegate
            {
                while (Loading)
                {
                    if (display1.IsHandleCreated)
                    {
                        display1.Invoke((MethodInvoker)delegate ()
                        {
                            int periods = display1.Display1.Text.Count(x => x == '.');
                            if (periods > 20) { periods = 0; }
                            display1.Display1.Clear();
                            display1.Display1.Text = "Scanning Entries" + string.Concat(Enumerable.Repeat(".", periods + 1));
                            if (dictionary != -1) { display1.Display1.AppendText(Environment.NewLine + "Scanning existing entries: " + dictionary + " remaining"); }
                            if (sentence != -1) { display1.Display1.AppendText(Environment.NewLine + "Scanning existing sentences: " + sentence + " remaining"); }
                            if (newentries != -1) { display1.Display1.AppendText(Environment.NewLine + "Checking for new entries: " + newentries + " remaining"); }
                            if (newsentences!= -1) { display1.Display1.AppendText(Environment.NewLine + "Checking for new Sentences: " + newsentences + " remaining"); }
                            if (writecount != -1) { display1.Display1.AppendText(Environment.NewLine + "Adding new entries: " + writecount + " remaining"); }
                            display1.Display1.Update();
                        });
                    }
                    Thread.Sleep(100);
                }
            }, null);

            DictionaryDatatemp = DictionaryEntry.ReadDictionaryData();
            SentenceDatatemp = SentenceEntry.ReadSentenceData();
            if (Utilities.ProcessNew())
            {
                DictionaryDatatemp = DictionaryEntry.ReadDictionaryData();
                SentenceDatatemp = SentenceEntry.ReadSentenceData();
            }

            Dictionary<string[], DictionaryEntry> DictionaryDataPinyintemp = new Dictionary<string[], DictionaryEntry>();
            Dictionary<string[], DictionaryEntry> DictionaryExactWordtemp = new Dictionary<string[], DictionaryEntry>();
            Dictionary<string, DictionaryEntry> DictionaryContainingWordtemp = new Dictionary<string, DictionaryEntry>();
            Dictionary<string, SentenceEntry> SentenceContainingWordtemp = new Dictionary<string, SentenceEntry>();


            foreach (DictionaryEntry item in DictionaryDatatemp)
            {
                DictionaryDataPinyintemp.Add(item.pinyinlatin, item);
                if (item.cleandefinition != null) { DictionaryExactWordtemp.Add(item.cleandefinition.ToArray(), item); }
                if (item.wordsearchdefintion != null) { DictionaryContainingWordtemp.Add((random.Next().ToString() + item.wordsearchdefintion), item); }
            }

            foreach (SentenceEntry item in SentenceDatatemp)
            {
                if (!SentenceContainingWordtemp.ContainsKey(item.englishwordseparated)) { SentenceContainingWordtemp.Add(item.englishwordseparated, item); }
            }

            while (DataAccessInProgress) { Thread.Sleep(1); }
            DataAccessInProgress = true;
            DictionaryData = DictionaryDatatemp;
            SentenceData = SentenceDatatemp;
            DictionaryDataPinyin = DictionaryDataPinyintemp;
            DictionaryExactWord = DictionaryExactWordtemp;
            DictionaryContainingWord = DictionaryContainingWordtemp;
            SentenceContainingWord = SentenceContainingWordtemp;
            DataAccessInProgress = false;
            Utilities.GetNews();
            Loading = false;
            FullReset = false;
        }


        public static void createIcon(bool run)
        {
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Open Server", ((sender, e) => display1.BeginInvoke((MethodInvoker)delegate () { display1.Visible = true; })));
            trayMenu.MenuItems.Add("Exit", ((sender, e) => Environment.Exit(1)));
            trayIcon.DoubleClick += new System.EventHandler((sender, e) => display1.BeginInvoke((MethodInvoker)delegate ()
            {
                display1.Visible = true;
                display1.WindowState = FormWindowState.Normal;
            }));
            trayIcon.BalloonTipClosed += (sender, e) => { var thisIcon = (NotifyIcon)sender; };
            trayIcon.BalloonTipClicked += new System.EventHandler((sender, e) =>
            display1.BeginInvoke((MethodInvoker)delegate ()
            {
                display1.Visible = true;
                display1.WindowState = FormWindowState.Normal;
            }));
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            if (run) { Application.Run(); }
        }

        public static void WaitForConnections()
        {
            listener.Prefixes.Add("http://*:" + Port + "/");
            listener.Start();
            Thread iconthread = new Thread(() => createIcon(true));
            iconthread.Name = "IconThread";
            iconthread.Start();
            while (true)
            {
                while (true)
                {
                    HttpListenerContext s = listener.GetContext();
                    HttpListenerResponse response = s.Response;
                    if (BanList.Contains(s.Request.RemoteEndPoint.Address.ToString()))
                    {
                        response.Abort();
                        continue;
                    }
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With, user, password");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    response.AppendHeader("Content-Type", "text/plain;charset=UTF-8");
                    response.ContentEncoding = Encoding.UTF8;
                    totalconnections++;
                    SpawnNewClient = new Thread(() => NewClient(s));
                    threadlist.Add(SpawnNewClient);
                    SpawnNewClient.Start();
                }
            }
        }

        static void NewClient(HttpListenerContext context)
        {
            timer.Restart();
            HttpListenerRequest query = context.Request;
            if (query.HttpMethod == "OPTIONS")
            {
                Request.Send(context, "", null);
                return;
            }
            string data = "";
            string IP = query.RemoteEndPoint.Address.ToString();
            while (CurrentIPList.Count(x => x == IP) > 2) { Thread.Sleep(100); }
            CurrentIPList.Add(IP);
            try
            {
                using (StreamReader reader = new StreamReader(query.InputStream, query.ContentEncoding)) { data = reader.ReadToEnd(); }
            }
            catch
            {
                if (CurrentIPList.Contains(IP)) { CurrentIPList.Remove(IP); }
                return;
            }
            if (!string.IsNullOrWhiteSpace(data))
            {
                Request request = Request.ProcessRequest(data, IP);
                if (request != null)
                {
                    if (Loading)
                    {
                        if (FullReset) { Request.Send(context, "fullreset", request); }
                        else { Request.Send(context, "quickreset", request); }
                    }
                    else
                    {
                        Request.HandleRequest(context, request);
                        if (!IPList.Contains(IP)) { uniqueconnections++; }
                        IPList.Add(IP);
                    }
                }
                else { Request.Send(context, "error", request); }
            }
            if (CurrentIPList.Contains(IP)) { CurrentIPList.RemoveAll(x => x == IP); }
            timer.Stop();
        }


    }


}
