using HtmlAgilityPack;

namespace ShortTrackOnlineScraper.Model
{
    internal class CountryPage
    {
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
            var table = htmlDocument.DocumentNode.SelectSingleNode("//table[../h3[text() = 'Athlete Biographies']]");
            var tableRowsOfInterest = table?.SelectNodes(".//tr[./td/b[text() = 'Men']]/following-sibling::tr");
            if (tableRowsOfInterest is { Count: > 0 })
            {
                foreach (var tableRow in tableRowsOfInterest)
                {
                    var anchor = tableRow.SelectSingleNode("./td/a");
                    if (anchor != null)
                    {
                        var skaterName = anchor.InnerText;
                        var nameParts = skaterName.Split(",");

                        if (nameParts.Length < 2)
                        {
                            throw new FormatException($"name {skaterName} is of unexpected format");
                        }

                        var firstname = string.Join(", ", nameParts.Skip(1).Select(p => p.Trim()));

                        skaters.Add(
                            new Skater(firstname, 
                                nameParts[0].Trim(), 
                                country,
                                anchor.GetAttributeValue("href", null)));
                    }
                }
            }

            return skaters;
        }
    }
}
