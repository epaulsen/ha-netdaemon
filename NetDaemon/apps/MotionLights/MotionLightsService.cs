using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyNetDaemon.apps.Common;

namespace MyNetDaemon.apps.MotionLights;

//[NetDaemonApp]
public class MotionLightsService
{
    private readonly IHaContext _ha;
    private readonly ILogger<MotionLightsService> _logger;
    private readonly MqttLightClient _mqttLightClient;

    public MotionLightsService(MqttLightClient mqttLightClient, IHaContext ha, ILogger<MotionLightsService> logger)
    {
        _mqttLightClient = mqttLightClient;
        _ha = ha;
        _logger = logger;
        _mqttLightClient.Messages.SubscribeAsync(ProcessMessageAsync);
    }

    public Task ProcessMessageAsync(MqttMessage message)
    {
        if (message.Topic.Contains("bevegelse", StringComparison.InvariantCultureIgnoreCase))
        {
            var payloadString = Encoding.UTF8.GetString(message.Payload.Array);
            _logger.LogInformation("Motion '{topic}' payload:'{payloadString}'", message.Topic, payloadString);
        }
        return Task.CompletedTask;
    }
}


public class MotionStateStore
{

}