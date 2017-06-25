using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace DictionaryServer
{
    public class Request
    {
        public Stopwatch timer;
        public string IP { get; set; }
        public string User { get; set; }
        public string RequestType { get; set; }
        public string SearchText { get; set; }
        public int WordPage { get; set; }
        public int SentencePage { get; set; }
        public string UserLanguage { get; set; }
        public string FullRequest { get; set; }
        public string SpecialClientMessage { get; set; }

        public static Request ProcessRequest(string data, string ip)
        {
            Request TCPIn = new Request();
            TCPIn.timer = new Stopwatch();
            TCPIn.timer.Start();
            TCPIn.IP = ip;
            try
            {
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                string[] rawParams = data.Split('&');
                foreach (string param in rawParams)
                {
                    string[] kvPair = param.Split('=');
                    string key = kvPair[0];
                    string value = HttpUtility.UrlDecode(kvPair[1]);
                    postParams.Add(key, value);
                }
                if (postParams.ContainsKey("requesttype") && !string.IsNullOrWhiteSpace(postParams["requesttype"])) { TCPIn.RequestType = postParams["requesttype"]; }
                if (TCPIn.RequestType != "word" && TCPIn.RequestType != "phrase" && TCPIn.RequestType != "news" && TCPIn.RequestType != "lastsearch") { return null; }
                if (postParams.ContainsKey("searchtext") && !string.IsNullOrWhiteSpace(postParams["searchtext"])) { TCPIn.SearchText = postParams["searchtext"]; }
                if (TCPIn.SearchText == null || TCPIn.SearchText.Length > 200) { return null; }
                if (postParams.ContainsKey("wordpage") && !string.IsNullOrWhiteSpace(postParams["wordpage"])) { TCPIn.WordPage = Convert.ToInt32(postParams["wordpage"]); }
                if (postParams.ContainsKey("sentencepage") && !string.IsNullOrWhiteSpace(postParams["sentencepage"])) { TCPIn.SentencePage = Convert.ToInt32(postParams["sentencepage"]); }
                TCPIn.FullRequest = string.Join("↔", postParams.Values);
                DateTime now = DateTime.Now;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try {
                        TCPIn.User = TCPIn.IP + ReturnLocation(TCPIn.IP) + '\t' + TCPIn.RequestType + '\t' + TCPIn.SearchText + '\t' + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
                        Program.UserList.Add(TCPIn.User);
                    } catch { TCPIn.User = "error"; }
                });
                return TCPIn;
            }
            catch { return null; }
        }

        public static string ReturnLocation(string ip)
        {
            string info = new WebClient().DownloadString("http://ipinfo.io/" + ip);
            string country = "unknown";
            string city = "unknown";
            try { country = info.Replace("\"", "").Split(new string[] { "country:" }, StringSplitOptions.None)[1].Split(',')[0].Trim(); } catch { }
            try { city = info.Replace("\"", "").Split(new string[] { "city:" }, StringSplitOptions.None)[1].Split(',')[0].Trim(); } catch { }
            return "(" + city + ", " + country + ")";
        }

        public static void Send(HttpListenerContext context, string data, Request request)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                context.Response.ContentLength64 = buffer.Length;
                Stream output = context.Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                request.timer.Stop();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    while(request.User == null && request.timer.ElapsedMilliseconds < 5000) { Thread.Sleep(10); }
                    File.AppendAllText(Program.userlog, request.User + "\t" + request.timer.ElapsedMilliseconds + "ms\t" + Regex.Replace(request.FullRequest, "↔+", "↔") + Environment.NewLine); 
                });
            }
            catch { }
        }

        public static bool HandleRequest(HttpListenerContext context, Request request)
        {
            if (request.SearchText == null) { return true; }
            if (request.RequestType == "word")
            {
                string searchtext = request.SearchText.ToLower();
                List<DictionaryEntry> FoundWordData = new List<DictionaryEntry>();
                searchtext = searchtext.Replace("-", " ");
                searchtext = string.Join("", searchtext.Where(x => Utilities.GetScriptType(x) == Utilities.Script.CJKCharacters || Utilities.GetScriptType(x) == Utilities.Script.Latin || x == ' ')).Trim();
                if (string.IsNullOrWhiteSpace(searchtext))
                {
                    Send(context, "error", request);
                    return true;
                }

                if (searchtext.Any(x => Utilities.GetScriptType(x) == Utilities.Script.CJKCharacters))
                {
                    FoundWordData = Program.DictionaryData.Where(x => x.chinese.Contains(searchtext) || x.simplified.Contains(searchtext) || (x.alternate != null && x.alternate.Contains(searchtext))).ToList();
                    if(FoundWordData.Count == 0)
                    {
                        FoundWordData = Program.DictionaryData.Where(x => searchtext.Contains(x.chinese) || searchtext.Contains(x.simplified) || (x.alternate != null && searchtext.Contains(x.alternate))).ToList();
                    }
                    FoundWordData = FoundWordData.Distinct().OrderByDescending(x => x.frequency).OrderBy(x => x.chinese.Length).ToList();
                }
                else
                {
                    string toasciisearchtext = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(searchtext)).ToLower();
                    if (string.IsNullOrWhiteSpace(toasciisearchtext))
                    {
                        Send(context, "error", request);
                        return true;
                    }
                    string pinyintemp = toasciisearchtext.Replace(" ", "");
                    FoundWordData.AddRange(Program.DictionaryDataPinyin.Keys.Where(x => x[0] == pinyintemp || x[1] == pinyintemp).Select(y => Program.DictionaryDataPinyin[y]));
                    if (searchtext.Trim().Length > 1)
                    {
                        string exactwordtemp = searchtext.Replace(" ", "");
                        FoundWordData.AddRange(Program.DictionaryExactWord.Keys.Where(x => x.Contains(exactwordtemp)).Select(x => Program.DictionaryExactWord[x]).OrderByDescending(x => x.frequency));
                        string containswordtemp = searchtext.Replace("!", " ").Replace("-", " ").Replace("i.e.", "").Replace("e.g.", "").Replace("?", " ").Replace(",", " ").Replace(".", " ");
                        containswordtemp = " " + Regex.Replace(containswordtemp, @"\s+", " ").Trim() + " ";
                        FoundWordData.AddRange(Program.DictionaryContainingWord.Keys.Where(x => x.Contains(containswordtemp)).Select(x => Program.DictionaryContainingWord[x]).OrderByDescending(x => x.frequency));
                    }
                    FoundWordData = FoundWordData.Distinct().ToList();
                }
                if (FoundWordData.Count > 0) { Program.lastsearch = FoundWordData[0]; }
                string DataToSend = "";
                List<List<DictionaryEntry>> pages1 = Utilities.SplitListBy(FoundWordData, 4);
                if (pages1.Count > 0)
                {
                    if (pages1.Count > request.WordPage) { DataToSend = pages1.Count() + "⇔" + string.Join("↔", pages1[request.WordPage].Select(x => x.asstringsend)); }
                    else { DataToSend = "error"; }
                }
                else { DataToSend = "error"; }
                Send(context, DataToSend, request);
            }



            if (request.RequestType == "phrase")
            {
                string searchtext = request.SearchText.ToLower();
                searchtext = searchtext.Replace("-", " ");
                searchtext = string.Join("", searchtext.Where(x => Utilities.GetScriptType(x) == Utilities.Script.CJKCharacters || Utilities.GetScriptType(x) == Utilities.Script.Latin || x == ' ')).Trim();
                if (string.IsNullOrWhiteSpace(searchtext))
                {
                    Send(context, "error", request);
                    return true;
                }

                List<SentenceEntry> FoundSentenceData = new List<SentenceEntry>();
                if (searchtext.Any(x => Utilities.GetScriptType(x) == Utilities.Script.CJKCharacters))
                {
                    FoundSentenceData = Program.SentenceData.Where(x => x.chinese.Contains(searchtext)).OrderBy(x => x.chinese.Length).ToList();
                    if (FoundSentenceData.Count == 0)
                    {
                        FoundSentenceData = Program.SentenceData.Where(x => x.simplified != null && x.simplified.Contains(searchtext)).OrderBy(x => x.chinese.Length).ToList();
                    }
                }
                else
                {
                    if (searchtext.Trim().Length > 1)
                    {
                        string containswordtemp = searchtext.Replace("!", " ").Replace("i.e.", "").Replace("e.g.", "").Replace("?", " ").Replace(",", " ").Replace(".", " ");
                        containswordtemp = " " + Regex.Replace(containswordtemp, @"\s+", " ").Trim() + " ";
                        FoundSentenceData = Program.SentenceContainingWord.Keys.Where(x => x.Contains(containswordtemp)).Select(x => Program.SentenceContainingWord[x]).ToList();
                    }
                }
                //FoundSentenceData = FoundSentenceData.Distinct().Select(x => new string[] { x[5], x[1], x[2], x[3], x[4] }).ToList() ;
                string DataToSend = "error no data";
                List<List<SentenceEntry>> pages2 = Utilities.SplitListBy(FoundSentenceData, 4);
                if (pages2.Count > 0)
                {
                    if (pages2.Count > request.SentencePage)
                    {
                        DataToSend = pages2.Count() + "⇔" + string.Join("↔", pages2[request.SentencePage].Select(x => string.Join("\t", x.asstringsend)));
                    }
                    else { DataToSend = "page count error"; }
                }
                else { DataToSend = "error no data"; }
                Send(context, DataToSend, request);
            }
            if (request.RequestType == "news")
            {
                Send(context, string.Join("↔", File.ReadAllLines(Path.Combine(Program.serverpath, "news.txt")).Select(x=>string.Join("\t",x.Split('\t')[2],x.Split('\t')[1]))), request);
            }
            if(request.RequestType == "lastsearch")
            {
                Send(context, string.Join("\t",Program.lastsearch.asstringsend), request);
            }
            return true;
        }

      

    }




}
