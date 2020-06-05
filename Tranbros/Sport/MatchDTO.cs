using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tranbros.Sport
{
    public class MatchDTO
    {
        //SETTING
        
        //

        private const int LocalTimeZone = 7;

        public MatchDTO()
        {
            this.Odds = new List<OddDTO>();
        }

        public string ID_Bongdalu
        {
            get;
            set;
        }

        public eMatchType MatchType
        {
            get;
            set;
        }

        public string KickOffTime
        {
            get { return ((KickOffHour + LocalTimeZone).ToString() + ":" + KickOffMin.ToString()); }
        }

        public int KickOffHour
        {
            get;
            set;
        }
        public int KickOffMin
        {
            get;
            set;
        }

        public string HomeTeamName
        {
            get;
            set;
        }
        public string AwayTeamName
        {
            get;
            set;
        }
        public string HomeScore
        {
            get;
            set;
        }
        public string AwayScore
        {
            get;
            set;
        }
        public string Score
        {
            get;
            set;
        }
        public bool hasLiveOdd { get; set; }

        public Uri OddLink
        {
            get { return new Uri("http://data.bongdalu.com/liveodds/24_" + this.ID_Bongdalu + ".html"); }
        }

        public int Minute
        {
            get;
            set;
        }

        public List<OddDTO> Odds
        {
            get; private set;
        }

        public string LeagueName
        {
            get; set;
        }

        public bool isLive
        {
            get
            {
                if (this.MatchType == eMatchType.H1 || this.MatchType == eMatchType.H2 || this.MatchType == eMatchType.HT)
                    return true;
                else
                    return false;
            }
        }
        public int TotalOverUnder
        {
            get
            {
                return (int.Parse(this.HomeScore) + int.Parse(this.AwayScore));
            }
        }

        public bool StrategyStatus
        {
            get; set;
        }
        public bool RedOU
        {
            get; set;
        }
        public bool RedHandicap
        {
            get; set;
        }

        public float GiaChuanOU
        {
            get;set;
        }
        public float GiaChuanHDC
        {
            get; set;
        }        

        public string SearchOdd()
        {
            string ret = string.Empty;

            this.GiaChuanHDC = GiaChuan(eOddType.Handicap);
            this.GiaChuanOU = GiaChuan(eOddType.OverUnder);

            var oddOverUnder = this.Odds.Where(o => o.Type == eOddType.OverUnder);
            var o1 = CoKeoDoOverUnder();
            if (o1!= null)
            {                
                int OddInt = int.Parse(o1.Odd);

                ret += "\r\n*Red Odd OU:" + o1.Odd + " in " + this.HomeTeamName + " - " + this.AwayTeamName + " @min:" + o1.AtTime  +" ,@ti so:" + o1.Score + " Now/Ending: " + HomeScore + ":" + AwayScore + " Gia chuan OU: " + GiaChuanOU;

                var o2 = CoKeoDoHandicap(o1);
                if (o2 != null)
                {
                    ret += "\r\n - Red Odd Handicap:" + o2.Odd  +" @ min:" + o2.AtTime + ", @tiso" + o2.Score + " @" + o2.Home + "/" + o2.Away +  " => Gia Chuan:" + GiaChuanHDC + " " + (o2.TongGia == GiaChuanHDC).ToString();
                    var o3 = CoKeoCoTheDanh(o1);
                    {
                        if (o3 != null)
                        {
                            ret += "\r\n ==> Ready to bet: " + o3.Odd + " @ min:" + o3.AtTime + " @" + o3.Home.ToString() + "/" + o3.Away.ToString();
                        }
                    }

                }
                
                this.StrategyStatus = (this.TotalOverUnder >= OddInt);
                
            }
            

            return ret;
        }

        public OddDTO CoKeoDoOverUnder()
        {
            var oddOverUnder = this.Odds.Where(o => o.Type == eOddType.OverUnder);
            
            foreach (OddDTO odd in oddOverUnder)
            {
                if (odd.Odd == "1" || odd.Odd == "2" || odd.Odd == "3" || odd.Odd == "4" || odd.Odd == "5" || odd.Odd == "6")
                {
                    if (odd.isRedOdd && !RedOU)
                    {
                        int OddInt = int.Parse(odd.Odd);
                        int totalscore = Utils.ScoreParse(odd.Score);
                        
                        if (OddInt - totalscore == 1)
                        {
                            this.RedOU = true;
                            return odd;
                        }
                        
                    }                    
                }                  
            }
            return null;
        }

        public OddDTO CoKeoDoHandicap(OddDTO keoOU)
        {
            var oddHandicap = this.Odds.Where(o => o.Type == eOddType.Handicap).Reverse();            
            foreach (OddDTO odd in oddHandicap)
            {
                int tg = 0;
                int tg2 = 0;
                int.TryParse(odd.AtTime, out tg);
                int.TryParse(keoOU.AtTime, out tg2);
                if (tg > tg2)
                {
                    if (odd.Score == keoOU.Score) // khi ti so can nhau
                        if (odd.isRedOdd) // co diem do tai Handicap o cung ti so
                        {
                            if (odd.Away + odd.Home == GiaChuanHDC)
                                this.RedHandicap = true;
                            return odd;
                        }
                }
            }
            return null;
        }

        public OddDTO CoKeoCoTheDanh(OddDTO keoOU)
        {
            var oddOverUnder = this.Odds.Where(o => o.Type == eOddType.OverUnder);
            foreach (OddDTO odd in oddOverUnder)
            {
                float keohientai = Utils.OUParse(odd.Odd);
                float keoDo = Utils.OUParse(keoOU.Odd);

                int tg = 0;
                int tg2 = 0;
                int.TryParse(odd.AtTime, out tg);
                int.TryParse(keoOU.AtTime, out tg2);                

                if (keohientai < keoDo && tg > tg2)
                {
                    if (odd.Home == keoOU.Home)
                    {
                        return odd;
                    }
                }
            }
            return null;
        }

        public async Task<int> GetLiveOdd()
        {
            ////*[@id="div_l"]/table/tbody/tr[2]
            // //*[@id="div_l"]/table/tbody
            string response = await Utils.WebRequest(this.OddLink.ToString(), "");
            this.Odds.Clear();
            OddsParse(response, "//*[@id=\"div_l\"]//table", eOddType.Handicap);
            OddsParse(response, "//*[@id=\"div_d\"]//table", eOddType.OverUnder);

            Console.Write("");
            return 1;
        }

        public float GiaChuan(eOddType type)
        {
            float giachuan = 0;
            var odds = this.Odds.Where(o => o.Type == type);
            foreach (OddDTO odd in odds)
            {
                if (odd.Home == 1 || odd.Away == 1)
                {
                    giachuan = odd.Home + odd.Away;
                    if (type == eOddType.OverUnder)
                        this.GiaChuanOU = giachuan;
                    else if (type == eOddType.Handicap)
                        this.GiaChuanHDC = giachuan;                        
                }
                

            }
            return giachuan;                
        }

        private void OddsParse(string HTML, string node, eOddType type)
        {
            List<OddDTO> listOdds = new List<OddDTO>();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(HTML);

            var node1 = doc.DocumentNode.SelectSingleNode(node)
           .Descendants("tr")
           .Skip(2)
           .Where(tr => tr.Elements("td").Count() > 1);

            List<List<string>> table = node1.Select(tr => tr.Elements("td").Select(td => td.OuterHtml.Trim()).ToList())
            .ToList();

            foreach (List<string> lstOdd in table)
            {
                OddDTO odd = new OddDTO();
                odd.AtTime = Utils.StripHTML(lstOdd[0]);
                odd.Score = Utils.StripHTML(lstOdd[1]);
                float fl = 0;
                if (float.TryParse(Utils.StripHTML(lstOdd[2]), out fl))
                    odd.Home = fl;
                odd.Odd = Utils.StripHTML(lstOdd[3]);
                if (float.TryParse(Utils.StripHTML(lstOdd[4]), out fl))
                    odd.Away = fl;
                odd.OddTime = Utils.StripHTML(lstOdd[5]);
                odd.Status = Utils.StripHTML(lstOdd[6]);
                odd.Type = type;

                if (lstOdd[2].Contains("red"))
                    odd.isRedHome = true;
                if (lstOdd[3].Contains("red"))
                    odd.isRedOdd = true;
                if (lstOdd[4].Contains("red"))
                    odd.isRedAway = true;

                if (lstOdd[2].Contains("green"))
                    odd.isGreenHome = true;
                if (lstOdd[4].Contains("green"))
                    odd.isGreenAway = true;

                this.Odds.Add(odd);
            }
        }
    }
}
