using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ShortTrackSportResultScraper.Model
{
    internal class CountryPage
    {
        private static readonly Regex LastnameRegex = new("([A-Z-]{2,})");

        private readonly HtmlDocument htmlDocument;
        private readonly Country country;

        internal CountryPage(Country country, string html)
        {

            this.country = country;
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        internal List<Skater> ExtractSkaters()
        {
            var skaters = new List<Skater>();
            var parentDivNode = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'tabletitle')][./p[normalize-space()='Men']]/following-sibling::div");
            var anchorNodes = parentDivNode?.SelectNodes(".//table/tr/td/a");
            if (anchorNodes is { Count: > 0 })
            {
                foreach (var anchorNode in anchorNodes)
                {
                    var url = anchorNode.GetAttributeValue("href", null);
                    var rawName = anchorNode.InnerText.Trim();

                    var regexMatch = LastnameRegex.Match(rawName);
                    if (regexMatch.Success && regexMatch.Groups.Count > 1)
                    {
                        var lastname = regexMatch.Groups[1].Value.Trim();
                        var firstname = rawName.Replace(lastname, "").Trim();

                        skaters.Add(new Skater(firstname, lastname, country, url));
                    }
                    else
                    {
                        throw new FormatException($"name {rawName} is of unexpected format");
                    }

                }
            }

            return skaters;
        }
    }
}
