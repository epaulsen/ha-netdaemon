using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNetDaemon.apps.Autolights;
using MyNetDaemon.apps.Common;
using MyNetDaemon.apps.config;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Extensions.Tts;
using NetDaemon.Runtime;
// Add next line if using code generator
//using HomeAssistantGenerated;

#pragma warning disable CA1812

try
{
    await Host.CreateDefaultBuilder(args)
        .UseNetDaemonAppSettings()
        .UseNetDaemonDefaultLogging()
        .UseNetDaemonRuntime()
        .UseNetDaemonTextToSpeech()
        .ConfigureServices((_, services) =>
            services
                .AddAppsFromAssembly(Assembly.GetExecutingAssembly())
                .AddNetDaemonStateManager()
                .AddScoped<Z2mLightService>()
                .AddSingleton<MqttLightClient>()
                .AddSingleton<LightStateStore>()
                .AddHostedService<CurrentLightMqttBackgroundService>()
                .AddConfigService<AutolightConfigService, AutolightConfig>("autolights.yaml")
                .AddNetDaemonScheduler()
                // Add next line if using code generator
                // .AddHomeAssistantGenerated()
        )
        .Build()
        .RunAsync()
        .ConfigureAwait(false);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to start host... {e}");
    throw;
}