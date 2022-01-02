using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace ShortTrackOnlineScraper.Model
{
    internal class SkaterPage
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Regex TimeRegex = new Regex(@"((\d+):)?(\d+)\.(\d+)");

        private readonly HtmlDocument htmlDocument;
        private readonly Skater skater;

        internal SkaterPage(Skater skater, string html)
        {
            this.skater = skater;
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        internal SkaterBio? ExtractSkater()
        {
            var table = htmlDocument.DocumentNode.SelectSingleNode(
                "//table[../h3[starts-with(text(), 'Biographical data for')]]");
            var bioTable = table?.SelectSingleNode(".//table");

            if (bioTable == null)
            {
                Logger.Warn("found no bio table for {0}", skater);
            }
            else
            {
                var tableRows = bioTable.SelectNodes("./tr");
                if (tableRows is { Count: > 2 })
                {
                    var genderString = ExtractTableCellValue(tableRows[2]);
                    Enum.TryParse(genderString, true, out Gender gender);

                    var personalBests = ExtractPersonalBests();
                    

                    return new SkaterBio(skater.Firstname, skater.Lastname, skater.Country, gender,
                        personalBests, skater.Url);
                }
            }

            

            return null;
        }

        private Dictionary<int, TimeSpan> ExtractPersonalBests()
        {
            var personalBests = new Dictionary<int, TimeSpan>();

            foreach (var distance in new List<int> {500, 1000, 1500})
            {
                var tableCells = htmlDocument.DocumentNode.SelectNodes(
                    $"//tr/td[text() = '{distance} meter']/following-sibling::td");

                if (tableCells is { Count: > 1 })
                {
                    var rawTimeString = tableCells[0].InnerText + tableCells[1].InnerText;
                    var regexMatch = TimeRegex.Match(rawTimeString);
                    if (regexMatch.Success && regexMatch.Groups.Count > 4)
                    {
                        var minutes = 0;
                        if (!string.IsNullOrEmpty(regexMatch.Groups[2].Value))
                        {
                            minutes = int.Parse(regexMatch.Groups[2].Value);
                        }

                        var seconds = int.Parse(regexMatch.Groups[3].Value);
                        var milliseconds = int.Parse(regexMatch.Groups[4].Value.PadRight(3, '0'));

                        personalBests.Add(distance,
                            new TimeSpan(0, 0, minutes, seconds, milliseconds));
                    }
                }
                else
                {
                    Logger.Warn("failed to extract personal best for skater {0} and distance {1}", skater, distance);
                }
            }

            return personalBests;
        }

        private static string ExtractTableCellValue(HtmlNode tableRow)
        {
            var tableCell = tableRow.SelectSingleNode("./td[2]");
            if (tableCell != null)
            {
                return HttpUtility.HtmlDecode(tableCell.InnerText).Trim();
            }

            throw new InvalidOperationException("table row does not contain expected data");
        }
    }
}