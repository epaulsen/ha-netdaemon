using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MyNetDaemon.apps.Autolights;
using MyNetDaemon.apps.config;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel.Entities;

namespace MyNetDaemon.apps.Common;

internal class Z2mLightService
{
    private readonly MqttLightClient _mqttClient;
    private readonly Dictionary<string, LightData> _lightStateData = new();
    private readonly LightStateStore _lightStateStore;
    private readonly IHaContext _ha;
    private readonly INetDaemonScheduler _scheduler;
    private readonly ILogger<Z2mLightService> _logger;

    public Z2mLightService(IHaContext ha, INetDaemonScheduler scheduler, ILogger<Z2mLightService> logger, MqttLightClient mqttClient, LightStateStore lightStateStore)
    {
        _ha = ha;
        _scheduler = scheduler;
        _logger = logger;
        _mqttClient = mqttClient;
        _lightStateStore = lightStateStore;
    }

    public async Task SetState(LightConfig config, StateData state, bool force = false)
    {
        if (force)
        {
            await TurnOnInternalAsync(config, state);
            return;
        }

        var z2mState = await _lightStateStore.GetStateAsync(config.MqttTopic);
        
        if (_lightStateData.TryGetValue(config.EntityId, out var storedStateData))
        {
            if (!storedStateData.State.Equals(z2mState))
            {
                _logger.LogInformation("Light {lightEntityId} will not be changed, this has been changed manually.  Action is deferred for {overrideDelay}.", config.EntityId, state.OverrideTimeout);
                storedStateData.DeferredAction?.Dispose();

                async void DeferredAction()
                {
                    await TurnOnInternalAsync(config, state);
                }

                storedStateData.DeferredAction = _scheduler.RunIn(state.OverrideTimeout, DeferredAction);
                return;
            }
        }

        await TurnOnInternalAsync(config, state);
    }

    private async Task TurnOnInternalAsync(LightConfig config, StateData state)
    {
        await _mqttClient.SetState(config, state);
        var z2mState = JsonSerializer.Deserialize<LightState>(state.Z2mData!)!;
        _lightStateData[config.EntityId!] = new LightData { State = z2mState };
            
        // Todo: Delay and notify HA that we have been messing with the lights?
        return;
            
    }

}

internal class LightData
{
    public LightState State { get; set; }

    public int? Brightness { get; init; }

    public IDisposable? DeferredAction { get; set; }
}