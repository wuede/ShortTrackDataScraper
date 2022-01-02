using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ShortTrackLiveScraper.Model;

namespace ShortTrackLiveScraper;

class Program
{
    static async Task<int> Main(string[] args)
    {
       var builder = new HostBuilder()
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
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
                //await webScraper.RunAsync(new []{ new Country(41, "POL") });
                await webScraper.RunAsync(Country.AllCountries);
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