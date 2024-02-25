using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MyNetDaemon.apps.config;

namespace MyNetDaemon.apps.Common;

public class LightStateStore
{
    private readonly ConcurrentDictionary<string, LightStateStoreEntry> _states = new();
    private readonly MqttLightClient _mqttClient;

    public LightStateStore(AutolightConfigService configService, MqttLightClient mqttClient)
    {
        _mqttClient = mqttClient;
        _states = new ConcurrentDictionary<string, LightStateStoreEntry>();
        
        configService.ConfigChanges.SubscribeAsync(ConfigChangedAsync);
        _mqttClient.Messages.SubscribeAsync(ProcessMessageAsync);

    }

    private async Task ConfigChangedAsync(AutolightConfig config)
    {
        _states.Clear();

        var topics = config.Data?.Where(d=>!string.IsNullOrWhiteSpace(d.MqttTopic)).Select(d => d.MqttTopic);
        if (topics == null)
        {
            return;
        }

        foreach (var topic in topics)
        {
            _states[topic!] = new LightStateStoreEntry();
        }
    }

    public async Task<LightState>? GetStateAsync(string zigbeeTopic)
    {
        _states[zigbeeTopic] = new LightStateStoreEntry() {GetState = new TaskCompletionSource<LightState>()};

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{zigbeeTopic}/get")
            .WithPayload("{\"state\":\"\", \"brightness\": \"\"}"u8.ToArray())
            .Build();

        await _mqttClient.PublishAsync(applicationMessage);

        return await _states[zigbeeTopic].GetStateAsync;
    }

    public Task ProcessMessageAsync(MqttMessage message)
    {
        if (!_states.ContainsKey(message.Topic))
        {
            return Task.CompletedTask;
        }
        //Debug.WriteLine($"Processing '{message.Topic}' with message {Encoding.UTF8.GetString(payload.Array)}");


        var lightState = JsonSerializer.Deserialize<LightState>(message.Payload);
        if (_states.TryGetValue(message.Topic, out var state))
        {
            state.SetState(lightState);
            return Task.CompletedTask;
        }

        _states[message.Topic] = new LightStateStoreEntry() {LastUpdated = DateTimeOffset.UtcNow, State = lightState};
        return Task.CompletedTask;
    }

    private class LightStateStoreEntry
    {
        public LightState? State { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public void SetState(LightState state)
        {
            State = state;
            LastUpdated = DateTimeOffset.Now;
            if (GetState != null && !GetState.Task.IsCompleted)
            {
                GetState.SetResult(state);
            }
        }

        public Task<LightState> GetStateAsync => GetState?.Task;

        public TaskCompletionSource<LightState> GetState { get; set; } = new TaskCompletionSource<LightState>();
    }

}


