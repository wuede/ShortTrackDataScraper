using HtmlAgilityPack;

namespace ShortTrackLiveScraper.Model
{
    internal class CountryPage
    {
        private readonly HtmlDocument htmlDocument;

        internal CountryPage(string html)
        {
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        internal List<string> ExtractSkaterUrls()
        {
            var skaterUrls = new List<string>();
            var tables = htmlDocument.DocumentNode.SelectNodes("//div[@id='main']//table");
            if (tables is { Count: > 0 })
            {
                var table = tables.Count > 1 ? tables[1] : tables[0];
                var tableRows = table.SelectNodes("./tr");
                if (tableRows is { Count: > 3 })
                {
                    foreach (var tableRow in tableRows.Skip(3))
                    {
                        var tableCells = tableRow.SelectNodes("./td");
                        if (tableCells is { Count: > 0 })
                        {
                            var anchor = tableCells[0].SelectSingleNode("./a");
                            if (anchor != null)
                            {
                                skaterUrls.Add(anchor.GetAttributeValue("href", null));
                            }
                        }
                    }
                }
            }

            return skaterUrls;
        }
    }
}
