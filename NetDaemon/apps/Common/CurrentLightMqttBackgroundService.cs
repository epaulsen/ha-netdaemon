using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;

namespace MyNetDaemon.apps.Common;



internal class CurrentLightMqttBackgroundService : IHostedService
{
    private readonly MqttLightClient _client;


    public CurrentLightMqttBackgroundService(MqttLightClient client)
    {
        _client = client;
    }

    

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync(cancellationToken);
    }
}

