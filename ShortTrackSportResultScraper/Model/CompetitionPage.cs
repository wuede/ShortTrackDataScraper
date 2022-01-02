using HtmlAgilityPack;

namespace ShortTrackSportResultScraper.Model
{
    internal class CompetitionPage
    {
        private readonly HtmlDocument htmlDocument;

        internal CompetitionPage(string html)
        {
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        public List<string> ExtractCountryUrls()
        {
            var countryUrls = new List<string>();
            var parentDiv = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'navilevel0')][./p/b[text()='Available Teams']]//following-sibling::div");
            if (parentDiv != null)
            {
                var navItems = parentDiv.SelectNodes("//div[contains(@class, 'navilevel1')]");
                if (navItems is { Count: > 0 })
                {
                    foreach (var navItem in navItems)
                    {
                        var anchor = navItem.SelectSingleNode(".//a");
                        if (anchor != null)
                        {
                            var countryUrl = anchor.GetAttributeValue("href", "");

                            if (string.IsNullOrWhiteSpace(countryUrl))
                            {
                                throw new FormatException($"country url {countryUrl} is of invalid format");
                            }

                            countryUrls.Add(countryUrl);
                        }
                    }
                }
            }

            return countryUrls;
        }
    }
}