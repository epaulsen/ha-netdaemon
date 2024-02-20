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
    private readonly MqttLightClient _lightStateService;
    private readonly Dictionary<string, LightData> _lightStateData = new();
    private readonly IHaContext _ha;
    private readonly INetDaemonScheduler _scheduler;
    private readonly ILogger<Z2mLightService> _logger;

    public Z2mLightService(IHaContext ha, INetDaemonScheduler scheduler, ILogger<Z2mLightService> logger, MqttLightClient lightStateService)
    {
        _ha = ha;
        _scheduler = scheduler;
        _logger = logger;
        _lightStateService = lightStateService;
    }

    public async Task SetState(LightConfig config, StateData state, bool force = false)
    {
        //var state = config.Modes.SingleOrDefault(m => m.Name.Equals(lightMode, StringComparison.InvariantCultureIgnoreCase));
        if (force)
        {
            TurnOnInternal(config, state);
            return;
        }

        var z2mState = await _lightStateService.GetCurrentStateAsync(config.MqttTopic);
            

        if (_lightStateData.TryGetValue(config.EntityId, out var storedStateData))
        {
            if (!storedStateData.State.Equals(z2mState))
            {
                _logger.LogInformation("Light {lightEntityId} will not be changed, this has been changed manually.  Action is deferred for {overrideDelay}.", config.EntityId, state.OverrideDelay);
                storedStateData.DeferredAction?.Dispose();

                async void DeferredAction()
                {
                    await TurnOnInternal(config, state);
                }

                storedStateData.DeferredAction = _scheduler.RunIn(state.OverrideDelay, DeferredAction);
                return;
            }
        }

        await TurnOnInternal(config, state);
    }

    private async Task TurnOnInternal(LightConfig config, StateData state)
    {
        await _lightStateService.SetState(config, state);
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