using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DictionaryServer
{
    public class DictionaryEntry
    {
        public string chinese;
        public string simplified;
        public string alternate;
        public string zhuyin;
        public string pinyin;
        public string mainland;
        public string[] pinyinlatin;
        public string measureword;
        public List<string> definition;
        public List<string> cleandefinition;
        public string wordsearchdefintion;
        public int frequency = -1;

        public string asstringsend { get { return string.Join("\t", chinese, simplified, alternate, zhuyin, pinyin, mainland, measureword, definition != null ? string.Join("|", definition) : ""); } }
        public string asstringstore { get { return string.Join("\t", chinese, simplified, alternate, zhuyin, pinyin, mainland, measureword, definition != null ? string.Join("|", definition) : "", frequency); } }

        public static List<DictionaryEntry> ReadDictionaryData()
        {
            List<DictionaryEntry> entries = new List<DictionaryEntry>();
            Program.dictionary = File.ReadAllLines(Path.Combine(Program.serverpath, "dictionaryfinal.txt")).Count();
            foreach (string[] item in File.ReadAllLines(Path.Combine(Program.serverpath, "dictionaryfinal.txt")).Select(x => x.Split('\t')))
            {
                Program.dictionary--;
                DictionaryEntry entry = new DictionaryEntry();
                entry.chinese = item[0];
                entry.simplified = item[1];
                if (string.IsNullOrWhiteSpace(item[2])) { entry.alternate = null; }
                else { entry.alternate = item[2]; }
                entry.zhuyin = item[3];
                entry.pinyin = item[4];
                if (string.IsNullOrWhiteSpace(item[5])) { entry.mainland = null; }
                else { entry.mainland = item[5]; }
                entry.pinyinlatin = new string[] { Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(entry.pinyin)).ToLower().Replace(" ", ""), entry.mainland != null ? Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(entry.mainland)).ToLower().Replace(" ", "") : null };
                if (string.IsNullOrWhiteSpace(item[6])) { entry.measureword = null; }
                else { entry.measureword = item[6]; }
                if (!string.IsNullOrWhiteSpace(item[7]))
                {
                    entry.definition = item[7].Split('|').ToList();
                    entry.cleandefinition = new List<string>();
                    foreach (string def in entry.definition)
                    {
                        entry.cleandefinition.Add(def.ToLower().Replace("!", "").Replace("?", "").Replace(",", "").Replace("-", "").Replace(".", "").Replace(" ", ""));
                    }
                    entry.wordsearchdefintion = Regex.Replace(item[7].ToLower().Insert(0, " ").Insert(item[7].Length > 1 ? (item[7].Length + 2) - 1 : 0, " ").Replace("!", " ").Replace("-", " ").Replace("i.e.", "").Replace("e.g.", "").Replace("?", " ").Replace(",", " ").Replace(".", " ").Replace("|", " "), @"\s+", " ");
                }
                else
                {
                    entry.definition = null;
                    entry.cleandefinition = null;
                    entry.wordsearchdefintion = null;
                }
                if (!string.IsNullOrWhiteSpace(item[8])) { entry.frequency = Convert.ToInt32(item[8]); }
                if (string.IsNullOrWhiteSpace(entry.simplified))
                { entry.simplified = entry.mainland; }
                entries.Add(entry);
            }

            if (entries.Any(x => x.frequency == -1))
            {
                Program.dictionary = entries.Count();
                foreach (DictionaryEntry pp1 in entries)
                {
                    Program.dictionary--;
                    if (pp1.frequency == -1) { pp1.frequency = File.ReadAllLines(Path.Combine(Program.serverpath, "processedsentences.txt")).Where(x => x.Split('\t')[0].Contains(pp1.chinese) || (pp1.alternate != null && x.Split('\t')[0].Contains(pp1.alternate))).Count(); }
                }
                File.WriteAllLines(Path.Combine(Program.serverpath, "temp.txt"), entries.Select(x => x.asstringstore));
                File.Copy(Path.Combine(Program.serverpath, "temp.txt"), Path.Combine(Program.serverpath, "dictionaryfinal.txt"),true);
            }
            entries.OrderByDescending(x => x.frequency);
            entries.OrderBy(x => x.chinese.Length);
            return entries;
        }

        public static void WriteDictionaryData(List<DictionaryEntry> entries)
        {
            if (entries.Any(x => x.frequency == -1))
            {
                foreach (DictionaryEntry pp1 in entries)
                {
                    if (pp1.frequency == -1) { pp1.frequency = Program.SentenceDatatemp.Where(x => !string.IsNullOrWhiteSpace(x.mouseover) && (x.chinese.Contains(pp1.chinese) || (pp1.alternate != null && x.chinese.Contains(pp1.alternate)))).Count(); }
                }
            }
            entries.OrderByDescending(x => x.frequency);
            entries.OrderBy(x => x.chinese.Length);
            File.WriteAllLines(Path.Combine(Program.serverpath, "temp.txt"), entries.Select(x => x.asstringstore));
            File.Copy(Path.Combine(Program.serverpath, "temp.txt"), Path.Combine(Program.serverpath, "dictionaryfinal.txt"), true);
        }
    }
    public class SentenceEntry
    {
        public static List<DictionaryEntry> sentencedicdatatemp;
        public string chinese;
        public string simplified;
        public string english;
        public string pinyin;
        public string mainland;
        public string mouseover;
        public string englishwordseparated;

        public string asstringstore { get { return string.Join("\t", chinese, simplified, pinyin, mainland, english); } }
        public string asstringsend { get { return string.Join("\t", mouseover, english); } }

        public static List<SentenceEntry> ReadSentenceData()
        {
            List<SentenceEntry> entries = new List<SentenceEntry>();
            bool writeprocessed = false;
            Dictionary<string, string> t1 = new Dictionary<string, string>();
            if (File.Exists(Path.Combine(Program.serverpath, "processedsentences.txt")))
            {
                List<string> t2 = File.ReadAllLines(Path.Combine(Program.serverpath, "processedsentences.txt")).ToList();
                foreach (string ii in t2)
                {
                    if (!t1.ContainsKey(ii.Split('\t')[0])) { t1.Add(ii.Split('\t')[0], ii.Split('\t')[1]); }
                }
            }
            Program.sentence = File.ReadAllLines(Path.Combine(Program.serverpath, "sentencesfinal.txt")).Count();
            foreach (string[] item in File.ReadAllLines(Path.Combine(Program.serverpath, "sentencesfinal.txt")).Select(x => x.Split('\t')))
            {
                Program.sentence--;
                SentenceEntry entry = new SentenceEntry();
                entry.chinese = item[0];
                entry.english = item[4];
                entry.englishwordseparated = Regex.Replace(item[4].ToLower().Insert(0, " ").Insert(item[4].Length > 1 ? (item[4].Length + 2) - 1 : 0, " ").Replace("!", " ").Replace("-", " ").Replace("i.e.", "").Replace("e.g.", "").Replace("?", " ").Replace(",", " ").Replace(".", " ").Replace("|", " "), @"\s+", " ");
                
                string line = null;
                if (t1.TryGetValue(item[0], out line)) { entry.mouseover = line; }
                else
                {
                    entry.mouseover = Utilities.GetHoverMark(entry.chinese);
                    writeprocessed = true;
                }
                entries.Add(entry);
            }
            if (writeprocessed) { WriteSentenceData(entries); }
            return entries;
        }

        public static void WriteSentenceData(List<SentenceEntry> entries)
        {
            if (entries.Any(x => string.IsNullOrWhiteSpace(x.mouseover)))
            {
                Program.writecount = entries.Where(x=>x.mouseover==null).Count();
                foreach (SentenceEntry pp1 in entries)
                {
                    if (string.IsNullOrWhiteSpace(pp1.mouseover))
                    {
                        pp1.mouseover = Utilities.GetHoverMark(pp1.chinese);
                        Program.writecount--;
                    }
                }
            }
            entries.OrderByDescending(x => x.chinese.Length);
            File.WriteAllLines(Path.Combine(Program.serverpath, "temp.txt"), entries.Select(x => x.asstringstore));
            File.Copy(Path.Combine(Program.serverpath, "temp.txt"), Path.Combine(Program.serverpath, "sentencefinal.txt"),true);
            File.WriteAllLines(Path.Combine(Program.serverpath, "processedsentences.txt"), entries.Select(x => string.Join("\t", x.chinese, x.mouseover)));
        }

    }

    class Utilities
    {
        public static bool ProcessNew()
        {
            List<string> newlines = File.ReadAllLines(Path.Combine(Program.serverpath, "newentries.txt")).ToList();
            Program.newentries = newlines.Count();
            if (newlines.Count() != 0)
            {
                List<string> newitems = new List<string>();
                foreach (string item in newlines.ToList())
                {
                    newlines.Remove(item);
                    Program.newentries--;
                    try
                    {
                        string[] split = item.Split('\t');
                        string chinese = split[0];

                        DictionaryEntry t1 = Program.DictionaryDatatemp.FirstOrDefault(x => x.chinese == chinese || x.simplified == chinese ||(x.alternate!= null && x.alternate == chinese));
                        if ((split.Count() != 8 && split.Count() != 9) || t1 != null)
                        {
                            if ((split.Count() != 8 && split.Count() != 9) || t1.definition != null)
                            {
                                File.AppendAllText(Path.Combine(Program.serverpath, "rejected.txt"), item + Environment.NewLine);
                                continue;
                            }
                            else
                            {
                                t1.definition = split[7].Split('|').ToList();
                                Program.DictionaryDatatemp.RemoveAll(x => x.chinese == chinese || x.simplified == chinese || (x.alternate != null && x.alternate == chinese));
                                Program.DictionaryDatatemp.Add(t1);
                                Program.SentenceDatatemp.ForEach(x => { x.mouseover = x.chinese.Contains(chinese) ? null : x.mouseover; });
                                continue;
                            }
                        }
                        DictionaryEntry t2 = new DictionaryEntry();
                        t2.chinese = split[0];
                        t2.simplified = split[1];
                        t2.alternate = split[2];
                        t2.zhuyin = split[3];
                        t2.pinyin = split[4];
                        t2.mainland = split[5];
                        t2.measureword = split[6];
                        t2.definition = split[7].Split('|').ToList();
                        Program.DictionaryDatatemp.Add(t2);
                        Program.SentenceDatatemp.ForEach(x => { x.mouseover = x.chinese.Contains(chinese) ? null : x.mouseover; });

                    }
                    catch { File.AppendAllText(Path.Combine(Program.serverpath, "rejected.txt"), item + Environment.NewLine); }
                }
                DictionaryEntry.WriteDictionaryData(Program.DictionaryDatatemp);
                SentenceEntry.WriteSentenceData(Program.SentenceDatatemp);
                File.WriteAllLines(Path.Combine(Program.serverpath, "newentries.txt"), newlines);
                return true;
            }
            return false;
        }


        public static string GetHoverMark(string item)
        {
            string newitem = item;
            int count = newitem.Count(x => GetScriptType(x) == Script.CJKCharacters);
            foreach (DictionaryEntry dicitem in Program.DictionaryDatatemp)
            {
                if ((newitem.Contains(dicitem.chinese) || (dicitem.alternate != null && newitem.Contains(dicitem.alternate))) && dicitem.definition != null)
                {
                    string def = "-" + dicitem.definition[0];
                    if (dicitem.definition.Count() > 1) { def += "<br>-" + dicitem.definition[1]; }
                    if (dicitem.definition.Count() > 2) { def += "<br>-" + dicitem.definition[2]; }
                    if (dicitem.definition.Count() == 3) { def += "<br><span class='font4'> and 1 more definition...</span>"; }
                    if (dicitem.definition.Count() > 3) { def += "<br><span class='font4'> and " + (dicitem.definition.Count() - 3) + " more definitions...</span>"; }
                    string toreplace = "<div class='mytooltip' onclick=\"focussentenceword('" + dicitem.chinese + "');\">" + dicitem.chinese + "<div class='tooltiptext'>" + dicitem.chinese + (dicitem.simplified == dicitem.chinese ? "" : " (<span class='font9'>" + dicitem.simplified + "</span>)") + "<br><span class='font1'>" + dicitem.pinyin + "</span><span class='font2'><br><br>" + def + "</span></div></div>";
                    newitem = newitem.Replace(dicitem.chinese, GetUnicodeString(toreplace));
                    count = count - dicitem.chinese.Length;
                    if (count == 0) { break; }
                }
            }
            return Regex.Unescape(@newitem);
        }

        public static void GetNews()
        {
            WebClient w1 = new WebClient();
            w1.Encoding = Encoding.UTF8;
            foreach (string[] item in File.ReadAllLines(Path.Combine(Program.serverpath, "news.txt")).Select(x => x.Split('\t')))
            {
                if (item[1] != "") { File.AppendAllText(Path.Combine(Program.serverpath, "candidatesentences.txt"), string.Join("\t", item) + Environment.NewLine); }
            }
            List<string> NewsDataList = new List<string>();
            string Newsdata = w1.DownloadString("http://feeds.feedburner.com/cnaFirstNews");
            string[] split = Newsdata.Split(new string[] { "description>（" }, StringSplitOptions.None);
            foreach (string item in split)
            {
                try
                {
                    string item2 = string.Join("）", item.Split('）').Skip(1)).Split(new string[] { "&lt" }, StringSplitOptions.None)[0];
                    if (!string.IsNullOrWhiteSpace(item2)) { NewsDataList.Add(item2 + "\t" + "\t" + Utilities.GetHoverMark(item2) + "\t"); }
                    if (NewsDataList.Count() == 4) { break; }
                }
                catch { continue; }
            }
            File.WriteAllLines(Path.Combine(Program.serverpath, "news.txt"), NewsDataList);
        }


        public static List<List<T>> SplitListBy<T>(List<T> source, int chunkSize)
        {
            return source.Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / chunkSize).Select(x => x.Select(v => v.Value).ToList()).ToList();
        }

        public static string GetUnicodeString(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                sb.Append("\\u");
                sb.Append(String.Format("{0:x4}", (int)c));
            }
            return sb.ToString();
        }

        public enum Script { Latin, Cyrillic, Arabic, Hangul, Kana, CJKCharacters, Hebrew, Zhuyin, Unknown };
        public static Script GetScriptType(char c1)
        {
            if (char.IsHighSurrogate(c1) || char.IsLowSurrogate(c1)) { return Script.CJKCharacters; }
            if (c1 >= 65 && c1 <= 90) { return Script.Latin; }
            if (c1 >= 97 && c1 <= 122) { return Script.Latin; }
            if (c1 >= 192 && c1 < 255) { return Script.Latin; }
            if (c1 >= 256 && c1 < 383) { return Script.Latin; }
            if (c1 >= 384 && c1 < 591) { return Script.Latin; }
            if (c1 >= 592 && c1 < 687) { return Script.Latin; }
            if (c1 >= 688 && c1 < 697) { return Script.Latin; }
            if (c1 >= 736 && c1 < 740) { return Script.Latin; }
            if (c1 >= 1024 && c1 < 1279) { return Script.Cyrillic; }
            if (c1 >= 1280 && c1 < 1327) { return Script.Cyrillic; }
            if (c1 >= 1424 && c1 < 1535) { return Script.Hebrew; }
            if (c1 >= 1536 && c1 < 1791) { return Script.Arabic; }
            if (c1 >= 1872 && c1 < 1919) { return Script.Arabic; }
            if (c1 >= 2208 && c1 < 2303) { return Script.Arabic; }
            if (c1 >= 4352 && c1 <= 4607) { return Script.Hangul; }
            if (c1 >= 7424 && c1 < 7551) { return Script.Latin; }
            if (c1 == 7467) { return Script.Cyrillic; }
            if (c1 == 7544) { return Script.Cyrillic; }
            if (c1 >= 7552 && c1 < 7615) { return Script.Latin; }
            if (c1 >= 7680 && c1 < 7935) { return Script.Latin; }
            if (c1 >= 8448 && c1 < 8527) { return Script.Latin; }
            if (c1 >= 8528 && c1 < 8591) { return Script.Latin; }
            if (c1 >= 11360 && c1 < 11391) { return Script.Latin; }
            if (c1 >= 11744 && c1 < 11775) { return Script.Cyrillic; }
            if (c1 == 12295) { return Script.CJKCharacters; }
            if (c1 >= 12352 && c1 <= 12543) { return Script.Kana; }
            if (c1 >= 12549 && c1 <= 12589) { return Script.Zhuyin; }
            if (c1 >= 12592 && c1 <= 12687) { return Script.Hangul; }
            if (c1 >= 13312 && c1 <= 19903) { return Script.CJKCharacters; }
            if (c1 >= 19968 && c1 <= 40959) { return Script.CJKCharacters; }
            if (c1 >= 42560 && c1 < 42655) { return Script.Cyrillic; }
            if (c1 >= 42784 && c1 < 43007) { return Script.Latin; }
            if (c1 >= 43360 && c1 <= 43391) { return Script.Hangul; }
            if (c1 >= 43824 && c1 < 43887) { return Script.Latin; }
            if (c1 >= 44032 && c1 <= 55215) { return Script.Hangul; }
            if (c1 >= 55216 && c1 <= 55295) { return Script.Hangul; }
            if (c1 >= 63744 && c1 <= 64255) { return Script.CJKCharacters; }
            if (c1 >= 64256 && c1 < 64335) { return Script.Latin; }
            if (c1 >= 64285 && c1 < 64335) { return Script.Hebrew; }
            if (c1 >= 64336 && c1 < 65023) { return Script.Arabic; }
            if (c1 >= 65070 && c1 < 65071) { return Script.Cyrillic; }
            if (c1 >= 65136 && c1 < 65279) { return Script.Arabic; }
            if (c1 >= 65313 && c1 < 65339) { return Script.Latin; }
            if (c1 >= 65345 && c1 < 65371) { return Script.Latin; }
            if (c1 >= 69216 && c1 < 69247) { return Script.Arabic; }
            if (c1 >= 126464 && c1 < 126719) { return Script.Arabic; }
            if (c1 >= 131072 && c1 <= 173791) { return Script.CJKCharacters; }
            if (c1 >= 173824 && c1 <= 177983) { return Script.CJKCharacters; }
            if (c1 >= 177984 && c1 <= 178207) { return Script.CJKCharacters; }
            if (c1 >= 178208 && c1 <= 183983) { return Script.CJKCharacters; }
            if (c1 >= 194560 && c1 <= 195103) { return Script.CJKCharacters; }
            return Script.Unknown;
        }





    }
}
