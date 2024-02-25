using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using MyNetDaemon.apps.Autolights;
using MyNetDaemon.apps.config;

namespace MyNetDaemon.apps.Common;

public record MqttMessage(string Topic, ArraySegment<byte> Payload);

public class MqttLightClient
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttLightClient> _logger;
    private readonly IMqttClient _client;
    private readonly MqttFactory _mqttFactory;
    private readonly MqttClientOptions _options;


    private readonly Subject<MqttMessage> _mqttMessages = new();

    public IObservable<MqttMessage> Messages => _mqttMessages;

    public MqttLightClient(IConfiguration configuration, ILogger<MqttLightClient> logger)
    {
        _configuration = configuration;
        _logger = logger;


        var host = _configuration.GetValue<string>("HomeAssistant:Host") ?? throw new ApplicationException("Homeassistant host name not specified in configuration!!");

        _mqttFactory = new MqttFactory();
        _client = _mqttFactory.CreateMqttClient();
        
        _client.ApplicationMessageReceivedAsync += async (e) =>
        {
            var topic = e.ApplicationMessage.Topic;
            var entity = string.Join("/", topic.Split('/').Take(2));

            if (!entity.StartsWith("zigbee2mqtt/"))
            {
                logger.LogWarning($"Unexpected topic '{e.ApplicationMessage.Topic}', discarding.");
                return;
            }

            _mqttMessages.OnNext(new MqttMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload));
        };

        _client.DisconnectedAsync += async (e) =>
        {
            Ready = new TaskCompletionSource();
            logger.LogWarning("Disconnected from MQTT server, reconnecting.");
            await StartAsync(CancellationToken.None);
        };

        _options = new MqttClientOptionsBuilder().WithTcpServer(host, 1883).Build();
    }

    public async Task PublishAsync(MqttApplicationMessage message)
    {
        await Ready.Task;
        await _client.PublishAsync(message, CancellationToken.None);
    }

    public async Task SetState(LightConfig config, StateData state)
    {
        await Ready.Task;
        if (string.IsNullOrWhiteSpace(config.MqttTopic) || string.IsNullOrWhiteSpace(state.Z2mData))
        {
            _logger.LogWarning("Empty z2m topic or z2mdata for light {config.EntityId}, doing nothing here.", config.EntityId);
            return;
        }
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{config.MqttTopic}/set")
            .WithPayload(state.Z2mData)
        .Build();

        await _client.PublishAsync(applicationMessage, CancellationToken.None);
    }

    private TaskCompletionSource Ready { get; set; } = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool success = false;
        while (!success)
        {
            try
            {
                await _client.ConnectAsync(_options, cancellationToken);
                success = true;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Unable to connect to mqtt, received error '{e.Message}'");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        var muttSubscription = _mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic("zigbee2mqtt/#");

            })
            .Build();
        await _client.SubscribeAsync(muttSubscription, CancellationToken.None);
        Ready.SetResult();
        _logger.LogInformation("Connected to MQTT broker.  Querying lights for current state");
        
        _logger.LogInformation("Finished");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.DisconnectAsync(cancellationToken: cancellationToken);
        _client?.Dispose();
    }
}