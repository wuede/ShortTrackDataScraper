using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ShortTrackSportResultScraper.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ShortTrackSportResultScraper;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureLogging(loggingBuilder =>
            {
                // configure Logging with NLog
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog("nlog.config");
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddScoped<WebScraper>();
                services.AddHttpClient<WebScraper>()
                    .AddPolicyHandler(GetRetryPolicy());
            }).UseConsoleLifetime();

        var host = builder.Build();

        using var serviceScope = host.Services.CreateScope();
        {
            var services = serviceScope.ServiceProvider;

            try
            {
                var webScraper = services.GetRequiredService<WebScraper>();

                var seasonsOfInterest = new List<Season>
                {
                    new("99899800000029", "2011/2012"),
                    new("99899800000051", "2012/2013"),
                    new("99899800000054", "2013/2014"),
                    new("99899800000058", "2014/2015"),
                };

                await webScraper.RunAsync(seasonsOfInterest);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error occurred");
                Console.WriteLine(ex);
            }
        }

        return 0;
    }

    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }
}