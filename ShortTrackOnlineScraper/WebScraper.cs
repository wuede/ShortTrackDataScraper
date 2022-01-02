using ShortTrackOnlineScraper.Model;

namespace ShortTrackOnlineScraper;

internal class WebScraper
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


    private readonly HttpClient httpClient;

    public WebScraper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        httpClient.BaseAddress = new Uri("http://www.shorttrackonline.info");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
    }

    public async Task RunAsync()
    {
        var countryUrls = await ScrapeCountryOverviewPage();
        var skaters = await ScrapeCountryPages(countryUrls);

        //var skaters = new List<Skater>
        //    { new Skater("Roman", "GEDZUK", new Country("?country=KAZ"), "skaterbio.php?id=STKAZ10106199101") };
        var skaterBios = await ScrapeSkaterPages(skaters);
        ExportCsv(skaterBios);
    }

    private async Task<List<Country>> ScrapeCountryOverviewPage()
    {
        using var response = await httpClient.GetAsync("/athletes.php");
        var countryOverviewPage = new CountryOverviewPage(await response.Content.ReadAsStringAsync());
        var countries = countryOverviewPage.ExtractCountries();

        if (countries.Count == 0)
        {
            Logger.Error("could not find any country URLs");
        }

        return countries;
    }

    private async Task<List<Skater>> ScrapeCountryPages(IEnumerable<Country> countries)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var tasks = countries.Select(async country =>
        {
            await throttler.WaitAsync();
            try
            {
                var skaters = new List<Skater>();
                skaters.AddRange(await ScrapeCountryPage(country));
                return skaters;
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        var skaters = tasks.SelectMany(t => t.Result).ToList();

        Logger.Info("found {0} skater urls to fetch", skaters.Count);
        return skaters;
    }

    private async Task<List<Skater>> ScrapeCountryPage(Country country)
    {
        using var response = await httpClient.GetAsync("/athletes.php" + country.Url);
        var countryPage = new CountryPage(country, await response.Content.ReadAsStringAsync());
        var skaters = countryPage.ExtractSkaters();

        if (skaters.Count == 0)
        {
            Logger.Warn("found no skaters for country {0}", country.Code);
        }

        return skaters;
    }

    private async Task<List<SkaterBio>> ScrapeSkaterPages(IEnumerable<Skater> skaters)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var numFinishedTasks = 0;
        var tasks = skaters.Select(async skater =>
        {
            await throttler.WaitAsync();
            try
            {
                return await ScrapeSkaterPage(skater);
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

        var skaterBios = tasks.Select(t => t.Result).Where(s => s != null).ToList();
        return skaterBios!; // won't ever consist null entries
    }

    private async Task<SkaterBio?> ScrapeSkaterPage(Skater skater)
    {
        try
        {
            using var response = await httpClient.GetAsync(skater.Url);
            var skaterPage = new SkaterPage(skater, await response.Content.ReadAsStringAsync());
            return skaterPage.ExtractSkater();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }

        return null;
    }


    private static void ExportCsv(List<SkaterBio> skaters)
    {
        using var w = new StreamWriter("sto-skaters.csv");
        w.WriteLine("Id,Firstname,Lastname,Nationality,Gender,PB500,PB1000,PB1500,Url");

        foreach (var skater in skaters)
        {
            {
                var pb500 = skater.PersonalBests.GetValueOrDefault(500, TimeSpan.Zero);
                var pb1000 = skater.PersonalBests.GetValueOrDefault(1000, TimeSpan.Zero);
                var pb1500 = skater.PersonalBests.GetValueOrDefault(1500, TimeSpan.Zero);

                var line =
                    $"\"{skater.Id}\",\"{skater.Firstname}\",\"{skater.Lastname}\",{skater.Country.Code},{skater.Gender},{(pb500 == TimeSpan.Zero ? "" : pb500.TotalMilliseconds)},{(pb1000 == TimeSpan.Zero ? "" : pb1000.TotalMilliseconds)},{(pb1500 == TimeSpan.Zero ? "" : pb1500.TotalMilliseconds)},{skater.Url}";

                w.WriteLine(line);
                w.Flush();
            }
        }
    }
}