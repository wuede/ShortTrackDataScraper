using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ShortTrackSportResultScraper.Model
{
    internal class SkaterPage
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Regex TimeRegex = new(@"((\d+):)?(\d+)\.(\d+)");
        private static readonly Regex BirthdayRegex = new(@"(\d+ [A-Za-z]{3} \d{4})");

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
            var bioTable = htmlDocument.DocumentNode.SelectSingleNode(
                "//div[contains(@class, 'bio_personinfo_stats')]/table[contains(@class, 'maintable')]");

            if (bioTable == null)
            {
                Logger.Warn("found no bio table for {0}", skater);
            }
            else
            {
                var federationIdTableCellNode =
                    bioTable.SelectSingleNode(".//td[text()='FederationID']/following-sibling::td");
                var birthdayTableCellNode = bioTable.SelectSingleNode(".//td[text()='Born']/following-sibling::td");
                var genderTableCellNode = bioTable.SelectSingleNode(".//td[text()='Gender']/following-sibling::td");


                if (federationIdTableCellNode != null && birthdayTableCellNode != null && genderTableCellNode != null)
                {
                    var federationId = federationIdTableCellNode.InnerText.Trim();
                    var birthdayMatch = BirthdayRegex.Match(birthdayTableCellNode.InnerText.Trim());

                    if (birthdayMatch.Success && birthdayMatch.Groups.Count > 1 &&
                        DateTime.TryParse(birthdayMatch.Groups[1].Value, out var birthday))
                    {
                        Enum.TryParse<Gender>(genderTableCellNode.InnerText.Trim(), out var gender);

                        return new SkaterBio(federationId, skater.Firstname, skater.Lastname, birthday,
                            skater.Nationality, gender,
                            ExtractPersonalBests(), skater.Url);
                    }

                    throw new FormatException($"invalid birthday encountered for skater {skater}");
                }
            }

            return null;
        }

        private Dictionary<int, TimeSpan> ExtractPersonalBests()
        {
            var personalBests = new Dictionary<int, TimeSpan>();

            var pbTableRows = htmlDocument.DocumentNode.SelectNodes(
                $"//table[./tr/th[text()='Personal Bests']]/tr");

            if (pbTableRows is {Count: > 3})
            {
                foreach (var pbTableRow in pbTableRows.Skip(3))
                {
                    var tableCells = pbTableRow.SelectNodes("./td");
                    if (tableCells is { Count: > 3 })
                    {
                        var distance = int.Parse(tableCells[2].InnerText.Trim().Replace("m", ""));
                        var rawTime = tableCells[3].InnerText.Trim();

                        var regexMatch = TimeRegex.Match(rawTime);
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
                        else
                        {
                            throw new FormatException($"pb time {rawTime} of skater {skater} is of invalid format");
                        }
                    }
                }
            }
            else
            {
                Logger.Warn("failed to extract personal best for skater {0}", skater);
            }

            return personalBests;
        }
    }
}