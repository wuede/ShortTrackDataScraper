using System.Text.RegularExpressions;
using ShortTrackLiveScraper.Model;

namespace ShortTrackLiveScraper;

internal class WebScraper
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly Regex SkaterUrlRegex = new(@"index\.php\?skaterid=(\d+)&m=(\d+)&saison=(\d+)");

    private readonly HttpClient httpClient;

    public WebScraper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        httpClient.BaseAddress = new Uri("http://www.shorttracklive.info/");
    }

    public async Task RunAsync(Country[] countries)
    {
        var skaterUrls = await ScrapeCountryPages(countries);

        //var skaterUrls = new List<string>
        //    { "index.php?skaterid=717&m=12&saison=16" };
        var skaters = await ScrapeSkaterPages(skaterUrls);
        ExportCsv(skaters);
    }

    private async Task<List<string>> ScrapeCountryPages(IEnumerable<Country> countries)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var tasks = countries.Select(async country =>
        {
            await throttler.WaitAsync();
            try
            {
                var urls = new List<string>();
                urls.AddRange(await ScrapeCountryPage(country, true));
                urls.AddRange(await ScrapeCountryPage(country, false));
                return urls;
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        var skaterUrls = tasks.SelectMany(t => t.Result).ToList();

        Logger.Info("found {0} skater urls to fetch", skaterUrls.Count);
        return skaterUrls;
    }

    private async Task<List<string>> ScrapeCountryPage(Country country, bool fetchRetiredOnly)
    {
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("retired", fetchRetiredOnly ? "Y" : "N"),
            new KeyValuePair<string, string>("gender", "M"),
            new KeyValuePair<string, string>("member", "" + country.Id),
            new KeyValuePair<string, string>("modus", "14"),
            new KeyValuePair<string, string>("saison", "16"),
            new KeyValuePair<string, string>("squery", "search")
        });

        using var response = await httpClient.PostAsync("http://www.shorttracklive.info/index.php", formContent);
        var countryPage = new CountryPage(await response.Content.ReadAsStringAsync());
        var skaterUrls = countryPage.ExtractSkaterUrls();

        if (skaterUrls.Count == 0)
        {
            Logger.Warn("found no {0}skaters for country {1}", fetchRetiredOnly ? "retired " : "", country);
        }

        return skaterUrls;
    }

    private async Task<List<Skater>> ScrapeSkaterPages(IEnumerable<string> urls)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var numFinishedTasks = 0;
        var tasks = urls.Select(async url =>
        {
            await throttler.WaitAsync();
            try
            {
                return await ScrapeSkaterPage(url);
            }
            finally
            {
                throttler.Release();
                Interlocked.Increment(ref numFinishedTasks);
                if (numFinishedTasks % 100 == 0)
                {
                    Logger.Info("scraped {0} skater pages so far", numFinishedTasks);
                } 
            }
        }).ToList();
        await Task.WhenAll(tasks);

        var skaters = tasks.Select(t => t.Result).Where(s => s != null).ToList();
        return skaters!; // won't ever consist null entries
    }

    private async Task<Skater?> ScrapeSkaterPage(string url)
    {
        var urlMatch = SkaterUrlRegex.Match(url);
        if (urlMatch.Success && urlMatch.Groups.Count > 2)
        {
            var skaterId = urlMatch.Groups[1].Value;
            try
            {
                using var response = await httpClient.GetAsync(url);
                var skaterPage = new SkaterPage(url, await response.Content.ReadAsStringAsync());
                return skaterPage.ExtractSkater(skaterId);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        Logger.Warn("invalid skater url {0} received", url);
        return null;
    }

        

    private static void ExportCsv(List<Skater> skaters)
    {
        using var w = new StreamWriter("stl-skaters.csv");
        w.WriteLine("Firstname,Lastname,Nationality,Gender,YearOfBirth,AgeCategory,Club,Url");

        foreach (var skater in skaters)
        {
            {
                var line =
                    $"\"{skater.Firstname}\",\"{skater.Lastname}\",{skater.Country.Code},{skater.Gender},{skater.YearOfBirth},{skater.AgeCategory},{skater.Club},{skater.Url}";

                w.WriteLine(line);
                w.Flush();
            }
        }
    }
}