using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tranbros.Sport
{
    public static class Utils
    {
        static Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);
        public static string[] SplitCSV(string input)
        {

            List<string> list = new List<string>();
            string curr = null;
            foreach (Match match in csvSplit.Matches(input))
            {
                curr = match.Value;
                if (0 == curr.Length)
                {
                    list.Add("");
                }

                list.Add(curr.TrimStart(','));
            }

            return list.ToArray();
        }

        public static async Task<string> WebRequest(string url, string body)
        {
            using (var client = new HttpClient())
            {
                string content = string.Empty;
                if (body == "")
                {
                    var response = await client.GetAsync(url);
                    content = await response.Content.ReadAsStringAsync();
                }

                content = Regex.Unescape(content);
                return content;
            }
        }

        public static List<MatchDTO> MatchParse(string input)
        {
            List<MatchDTO> matches = new List<MatchDTO>();
            string[] liness = Regex.Split(input, "\r\n");

            DataTable dataTable = new DataTable();
            for (int i = 0; i < 45; i++)
            {
                dataTable.Columns.Add("C" + i.ToString());
            }

            List<List<string>> listAll = new List<List<string>>();

            foreach (String l in liness)
            {
                MatchDTO match = new MatchDTO();
                if (l.Contains("A["))
                {
                    List<string> listStr = new List<string>();
                    string output = l.Split('[', ']')[3];
                    string[] jsData = Utils.SplitCSV(output);

                    int matchtype = Int32.Parse(jsData[18]);
                    if (matchtype == 0)
                        match.MatchType = eMatchType.NotPlayYet;
                    else if (matchtype == 1)
                        match.MatchType = eMatchType.H1;
                    else if (matchtype == 2)
                        match.MatchType = eMatchType.HT;
                    else if (matchtype == 3)
                        match.MatchType = eMatchType.H2;
                    else if (matchtype == -1)
                        match.MatchType = eMatchType.FT;
                    else
                        match.MatchType = eMatchType.Unknown;

                    match.HomeTeamName = Utils.StripHTML(jsData[4]);
                    match.AwayTeamName = Utils.StripHTML(jsData[5]);
                    match.ID_Bongdalu = jsData[0];
                    match.HomeScore = jsData[19];
                    match.AwayScore = jsData[20];
                    match.KickOffHour = Int32.Parse(jsData[9]);
                    match.KickOffMin = Int32.Parse(jsData[10]);
                    if (jsData[36].Contains("True"))
                        match.hasLiveOdd = true;

                    matches.Add(match);

                    foreach (string s in jsData)
                    {
                        listStr.Add(s);
                    }

                    //dataTable.Rows.Add(listStr.ToArray());

                    listAll.Add(listStr);
                }
            }
            return matches;
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        public static float OUParse(string input)
        {
            if (input.Contains("/"))
            {                
                string[] a = input.Split('/');
                float f1 = (float.Parse(a[0]) + float.Parse(a[1]) / 2);
                return f1;
            }
            float f = 0;
            float.TryParse(input, out f);

            return f;            
        }
        public static int ScoreParse(string score)
        {
            if (score.Contains("-") && score != "-")
            {
                string[] a = score.Split('-');
                int i1 = (int.Parse(a[0]) + int.Parse(a[1]));
                return i1;
            }
            return 0;
        }
    }
}
