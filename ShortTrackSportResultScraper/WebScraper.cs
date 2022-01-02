using System.Net;
using ShortTrackSportResultScraper.Model;

namespace ShortTrackSportResultScraper;

internal class WebScraper
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


    private readonly HttpClient httpClient;

    public WebScraper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        httpClient.BaseAddress = new Uri("https://shorttrack.sportresult.com/");
    }

    public async Task RunAsync(List<Season> seasons)
    {
        var competitions = await ScrapeSeasonPages(seasons);
        var countryUrls = await ScrapeCompetitionPages(competitions);
        var skaters = await ScrapeCountryPages(countryUrls);
        /*var skaters = new List<Skater>
        {
            new Skater("José Ignacio", "Fazio", new Country("http://shorttrack.sportresult.com/Entries.aspx?nat=arg"),
                "http://shorttrack.sportresult.com/Bio.aspx?ath=7844&evt=11210300000005")
        };*/
        var skaterBios = await ScrapeSkaterPages(skaters);
        ExportCsv(skaterBios);
    }

    private async Task<List<Competition>> ScrapeSeasonPages(IEnumerable<Season> seasons)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var tasks = seasons.Select(async season =>
        {
            await throttler.WaitAsync();
            try
            {
                return await ScrapeSeasonPage(season);
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        var competitions = tasks.SelectMany(t => t.Result).ToList();

        Logger.Info("found {0} competitions to fetch", competitions.Count);

        return competitions;
    }

    private async Task<IEnumerable<Competition>> ScrapeSeasonPage(Season season)
    {
        using var response = await httpClient.GetAsync($"/Entries.aspx?sea={season.Id}");
        var seasonEntryPage = new SeasonEntryPage(await response.Content.ReadAsStringAsync());
        var competitions = seasonEntryPage.ExtractCompetitions();
        if (competitions.Count == 0)
        {
            Logger.Error("could not find any country competitions for season {0}", season);
        }

        return competitions;
    }

    private async Task<List<string>> ScrapeCompetitionPages(IEnumerable<Competition> competitions)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var tasks = competitions.Select(async competition =>
        {
            await throttler.WaitAsync();
            try
            {
                return await ScrapeCompetitionPage(competition);
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        var countryUrls = tasks.SelectMany(t => t.Result).ToList();

        Logger.Info("found {0} country urls to fetch", countryUrls.Count);

        return countryUrls;
    }

    private async Task<IEnumerable<string>> ScrapeCompetitionPage(Competition competition)
    {
        using var response = await httpClient.GetAsync($"/Entries.aspx?evt={competition.Id}");
        var competitionPage = new CompetitionPage(await response.Content.ReadAsStringAsync());
        var countryUrls = competitionPage.ExtractCountryUrls();
        if (countryUrls.Count == 0)
        {
            Logger.Error("could not find any country urls for competition {0}", competition);
        }

        return countryUrls;
    }

    private async Task<List<Skater>> ScrapeCountryPages(IEnumerable<string> countryUrls)
    {
        var throttler = new SemaphoreSlim(Environment.ProcessorCount - 1);
        var tasks = countryUrls.Select(async url =>
        {
            await throttler.WaitAsync();
            try
            {
                return await ScrapeCountryPage(url);
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

    private async Task<List<Skater>> ScrapeCountryPage(string url)
    {
        var country = new Country(url);
        using var response = await httpClient.GetAsync(url);
        var countryPage = new CountryPage(country, await response.Content.ReadAsStringAsync());
        var skaters = countryPage.ExtractSkaters();

        if (skaters.Count == 0)
        {
            Logger.Warn("found no skaters for country {0}", country);
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
            string html = null;
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var redirectUrl = response.Headers.Location;
                if (redirectUrl != null)
                {
                    // enforce HTTPS to prevent further redirects
                    var secureUri = new UriBuilder(redirectUrl)
                    {
                        Scheme = "https",
                        Port = 443
                    };

                    var redirectedResponse = await httpClient.GetAsync(secureUri.ToString());
                    html = await redirectedResponse.Content.ReadAsStringAsync();
                }
            }
            else
            {
                html = await response.Content.ReadAsStringAsync();
            }

            var skaterPage = new SkaterPage(skater, html);
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
        using var w = new StreamWriter("stsr-skaters.csv");
        w.WriteLine("Id,Firstname,Lastname,Nationality,Gender,PB500,PB1000,PB1500,Url");

        foreach (var skater in skaters)
        {
            {
                var pb500 = skater.PersonalBests.GetValueOrDefault(500, TimeSpan.Zero);
                var pb1000 = skater.PersonalBests.GetValueOrDefault(1000, TimeSpan.Zero);
                var pb1500 = skater.PersonalBests.GetValueOrDefault(1500, TimeSpan.Zero);

                var line =
                    $"\"{skater.Id}\",\"{skater.Firstname}\",\"{skater.Lastname}\",{skater.Birthday.ToString("yyyy/MM/dd")},{skater.Nationality.Code},{skater.Gender},{(pb500 == TimeSpan.Zero ? "" : pb500.TotalMilliseconds)},{(pb1000 == TimeSpan.Zero ? "" : pb1000.TotalMilliseconds)},{(pb1500 == TimeSpan.Zero ? "" : pb1500.TotalMilliseconds)},{skater.Url}";

                w.WriteLine(line);
                w.Flush();
            }
        }
    }
}