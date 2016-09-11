using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseNumberFireProjections
{
    class Program
    {
        private static string m_RemainingYearInputFileName = ConfigurationManager.AppSettings["yearlyInputFileName"];
        private static string m_RemainingYearOutputFileName = ConfigurationManager.AppSettings["yearlyOutputFileName"];
        private static string m_WeeklyOutputFileName = ConfigurationManager.AppSettings["weeklyOutputFileName"];

        private static string m_QbUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["qbPath"];
        private static string m_RbUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["rbPath"];
        private static string m_WrUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["wrPath"];
        private static string m_TeUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["tePath"];
        private static string m_KUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["kPath"];
        private static string m_DefUrl = ConfigurationManager.AppSettings["baseUrl"] + ConfigurationManager.AppSettings["defPath"];
        private static string m_Tab = "\t";

        static void Main(string[] args)
        {
            if (ConfigurationManager.AppSettings["mode"].ToLower() == "year") GetRemainingYearProjections();
            else GetWeeklyProjections();            
        }

        private static void GetWeeklyProjections()
        {
            var sb = new StringBuilder();
            sb.Append(GetWeeklyProjections(m_QbUrl));
            sb.Append(GetWeeklyProjections(m_RbUrl));
            sb.Append(GetWeeklyProjections(m_WrUrl));
            sb.Append(GetWeeklyProjections(m_TeUrl));
            sb.Append(GetWeeklyProjections(m_KUrl));
            sb.Append(GetWeeklyProjections(m_DefUrl));

            File.WriteAllText(m_WeeklyOutputFileName, sb.ToString());
        }

        private static StringBuilder GetWeeklyProjections(string url)
        {
            Console.WriteLine("Retrieving stats from " + url + " ...");
            var sb = new StringBuilder();

            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(url);
            var projectionTables = document.DocumentNode.SelectNodes("//tbody[contains(@class, 'projection-table__body')]");
            var projectionNames = document.DocumentNode.SelectNodes("//span[contains(@class, 'full')]");
            var positionTeams = projectionTables.First().ChildNodes.Where(p => p.Name == "tr").ToList();
            var projectionData = projectionTables.Last().ChildNodes.Where(p => p.Name == "tr");
            var index = 0;
            foreach (var row in projectionData)
            {
                var name = projectionNames[index].InnerText.Trim();
                var posTeamPair = positionTeams[index].ChildNodes.First(p => p.Name == "td").ChildNodes[2].InnerText.Trim();
                var position = GetPostionFromPair(posTeamPair);
                var team = GetTeamFromPair(posTeamPair);

                var overallRank = "0";
                var positionRank = "0";
                var completions = "0";
                var passAttempts = "0";

                var passYards = GetProjectionValue(row, "pass_yd");
                var passTds = GetProjectionValue(row, "pass_td");
                var passInts = GetProjectionValue(row, "pass_int");

                var rushAttempts = GetProjectionValue(row, "rush_att");
                var rushYards = GetProjectionValue(row, "rush_yd");
                var rushTds = GetProjectionValue(row, "rush_td");

                var recs = GetProjectionValue(row, "rec");
                var recYards = GetProjectionValue(row, "rec_yd");
                var recTds = GetProjectionValue(row, "rec_td");

                var fantasyPoints = GetProjectionValue(row, "nf_fp");

                index++;

                sb.Append(name).Append(m_Tab).Append(position).Append(m_Tab).Append(team).Append(m_Tab);
                sb.Append(overallRank).Append(m_Tab).Append(positionRank).Append(m_Tab);
                sb.Append(completions).Append(m_Tab).Append(passAttempts).Append(m_Tab);
                sb.Append(passYards).Append(m_Tab).Append(passTds).Append(m_Tab).Append(passInts).Append(m_Tab);
                sb.Append(rushAttempts).Append(m_Tab).Append(rushYards).Append(m_Tab).Append(rushTds).Append(m_Tab);
                sb.Append(recs).Append(m_Tab).Append(recYards).Append(m_Tab).Append(recTds).Append(m_Tab);
                sb.Append(fantasyPoints);
                sb.Append(Environment.NewLine);
            }
            return sb;
        }
        
        private static string GetTeam(HtmlNode row, string className)
        {
            var td = GetChildTdNode(row, className);
            var playerText = td.ChildNodes.First(p => p.Name == "a").InnerText;
            return GetTeamFromPair(playerText);
        }

        private static string GetTeamFromPair(string pair)
        {
            var startIndex = pair.IndexOf(",") + 1;
            var endIndex = pair.IndexOf(")");
            var team = pair.Substring(startIndex, endIndex - startIndex);
            return team;
        }

        private static string GetPosition(HtmlNode row, string className)
        {
            var td = GetChildTdNode(row, className);
            var playerText = td.ChildNodes.First(p => p.Name == "a").InnerText;
            return GetPostionFromPair(playerText);
        }

        private static string GetPostionFromPair(string pair)
        {
            var startIndex = pair.IndexOf("(") + 1;
            var endIndex = pair.IndexOf(",");
            var position = pair.Substring(startIndex, endIndex - startIndex);
            return position;
        }

        private static HtmlNode GetChildTdNode(HtmlNode parent, string className)
        {
            return parent.Descendants("td").FirstOrDefault(p => p.Attributes.Contains("class") &&
                                                             p.Attributes["class"].Value.Split(' ').Any(b => b.Equals(className)));
        }

        private static string GetPlayerName(HtmlNode row, string className)
        {
            var td = GetChildTdNode(row, className);
            var playerText = td.ChildNodes.First(p => p.Name == "a").InnerText;
            return playerText.Remove(playerText.IndexOf("(") - 1);
        }

        private static string GetProjectionValue(HtmlNode row, string className)
        {
            var cell = GetChildTdNode(row, className);
            return cell != null ? cell.InnerText.Trim() : "0";
        }

        private static void GetRemainingYearProjections()
        {
            var lines = File.ReadAllLines(m_RemainingYearInputFileName);
            var formattedLines = new List<string>();

            foreach (var line in lines)
            {
                var formattedLine = line;

                var openParenIndex = formattedLine.IndexOf('(');
                if (openParenIndex > -1)
                {
                    formattedLine = formattedLine.Remove(openParenIndex - 1, 1);
                    formattedLine = formattedLine.Replace('(', '\t');
                }

                var commaIndex = formattedLine.IndexOf(',');
                var closeParenIndex = formattedLine.IndexOf(')');
                if (commaIndex > -1 && closeParenIndex > -1)
                {
                    formattedLine = formattedLine.Remove(commaIndex, closeParenIndex - commaIndex + 1);
                }
                formattedLine = formattedLine.Replace('/', '\t');

                formattedLines.Add(formattedLine);
            }
            File.WriteAllLines(m_RemainingYearOutputFileName, formattedLines);
        }
    }
}
