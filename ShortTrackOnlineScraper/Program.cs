using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ShortTrackOnlineScraper;

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
                await webScraper.RunAsync();
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