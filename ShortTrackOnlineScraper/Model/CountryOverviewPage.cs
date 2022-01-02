using HtmlAgilityPack;

namespace ShortTrackOnlineScraper.Model
{
    internal class CountryOverviewPage
    {
        private readonly HtmlDocument htmlDocument;

        internal CountryOverviewPage(string html)
        {
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        internal List<Country> ExtractCountries()
        {
            var countries = new List<Country>();
            var table = htmlDocument.DocumentNode.SelectSingleNode("//table[../h3[text() = 'Athlete Biographies']]");
            if (table != null)
            {
                var anchors = table.SelectNodes(".//li/a");
                if (anchors is { Count: > 1 })
                {
                    foreach (var anchor in anchors)
                    {
                        countries.Add(new Country(anchor.GetAttributeValue("href", null)));
                    }
                }
            }

            return countries;
        }
    }
}
