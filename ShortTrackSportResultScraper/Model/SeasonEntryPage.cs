using HtmlAgilityPack;

namespace ShortTrackSportResultScraper.Model;

internal class SeasonEntryPage
{
    private readonly HtmlDocument htmlDocument;

    internal SeasonEntryPage(string html)
    {
        htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
    }

    public List<Competition> ExtractCompetitions()
    {
        var competitions = new List<Competition>();
        var optionNodes = htmlDocument.DocumentNode.SelectNodes("//select[@name='evt']//option");
        if (optionNodes is { Count: > 0 })
        {
            foreach (var optionNode in optionNodes)
            {
                var id = optionNode.GetAttributeValue("value", "");
                var name = optionNode.InnerText;

                competitions.Add(new Competition(id, name));
            }
           
        }

        return competitions;
    }
}