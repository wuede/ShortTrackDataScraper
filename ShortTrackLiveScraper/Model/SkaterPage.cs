using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace ShortTrackLiveScraper.Model
{
    internal class SkaterPage
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly HtmlDocument htmlDocument;
        private readonly string url;

        internal SkaterPage(string url, string html)
        {
            this.url = url;
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
        }

        internal Skater? ExtractSkater(string skaterId)
        {
            var bioTable = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='anz']//table");
            if (bioTable == null)
            {
                Logger.Warn("found no bio table for {0}", skaterId);
            }
            else
            {
                var tableRows = bioTable.SelectNodes("./tr");
                if (tableRows is { Count: > 7 })
                {
                    try
                    {
                        var rawName = ExtractTableCellValue(tableRows[1], 1, false);
                        // &nbsp; is used as a separator between firstname and lastname
                        var names = rawName.Split("  ");
                        if (names.Length != 2)
                        {
                            throw new InvalidOperationException("unexpected name format");
                        }

                        var firstname = names[1].Trim();
                        var lastname = names[0].Trim();
                        
                        var countryString = ExtractTableCellValue(tableRows[2], 1);
                        var country = Country.AllCountries.FirstOrDefault(c =>
                            c.Code.Equals(countryString, StringComparison.OrdinalIgnoreCase));
                        var genderString = ExtractTableCellValue(tableRows[3], 1);
                        Enum.TryParse(genderString, true, out Gender gender);

                        var ageString = ExtractTableCellValue(tableRows[4], 1);
                        var yearOfBirth = 0;
                        if (int.TryParse(ageString, out var age))
                        {
                            yearOfBirth = DateTime.Now.Year - age;
                        }

                        var ageCategory = ExtractTableCellValue(tableRows[5], 1);
                        //var homeTown = ExtractTableCellValue(tableRows[6], 1);
                        var club = ExtractTableCellValue(tableRows[7], 1);

                        if (country == null)
                        {
                            throw new InvalidOperationException("country may not be null");
                        }

                        return new Skater(firstname, lastname, country, gender, yearOfBirth, ageCategory, club,
                            new List<Tuple<int, TimeSpan>>(), url);
                    }
                    catch (InvalidOperationException e)
                    {
                        Logger.Error(e, "failed to create skater with id {0}", skaterId);
                    }
                }
            }

            return null;
        }

        private static string ExtractTableCellValue(HtmlNode tableRow, int index, bool htmlDecode = true)
        {
            var tableCells = tableRow.SelectNodes("./td");
            if (tableCells != null && tableCells.Count > index)
            {
                var strongText = tableCells[index].SelectSingleNode("./strong");
                if (strongText != null)
                {
                    if (htmlDecode)
                    {
                        return HttpUtility.HtmlDecode(strongText.InnerText).Trim();
                    }

                    return strongText.InnerText;
                }
            }

            throw new InvalidOperationException("table row does not contain expected data");
        }
    }
}
