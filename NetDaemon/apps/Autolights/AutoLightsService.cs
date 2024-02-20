using System.Linq;
using System.Threading.Tasks;
using MyNetDaemon.apps.Common;
using MyNetDaemon.apps.config;
using NetDaemon.Extensions.Scheduler;

namespace MyNetDaemon.apps.Autolights
{
    [NetDaemonApp]
    internal class AutoLightsService
    {
        private readonly IHaContext _ha;
        private readonly INetDaemonScheduler _scheduler;
        private readonly Z2mLightService _z2MLightService;
        private readonly AutolightConfigService _lightConfig;
        private readonly ILogger<AutoLightsService> _logger;

        private string _currentHouseMode = string.Empty;
        private string _currentLightMode = string.Empty;

        public AutoLightsService(
            IHaContext ha,
            INetDaemonScheduler scheduler,
            AutolightConfigService lightConfig,
            Z2mLightService z2MLightService,
            ILogger<AutoLightsService> logger)
        {
            _ha = ha;
            _scheduler = scheduler;
            _z2MLightService = z2MLightService;
            _lightConfig = lightConfig;
            _logger = logger;

            _currentHouseMode = _ha.Entity(_lightConfig.Config.HouseModeSensor).State ?? throw new ArgumentException($"House mode sensor '{_lightConfig.Config.HouseModeSensor}' not found.");
            _currentLightMode = _ha.Entity(_lightConfig.Config.ModeSensor).State ?? throw new ArgumentException($"Light mode sensor '{_lightConfig.Config.HouseModeSensor}' not found.");

            _ha.Entity(_lightConfig.Config.HouseModeSensor).StateAllChanges().SubscribeAsyncConcurrent(async e =>
            {
                if (e.New?.State == null)
                {
                    return;
                }
                
                await HouseModeChangedAsync(e.New?.State);
            });


            _ha.Entity(_lightConfig.Config.ModeSensor).StateAllChanges().SubscribeAsyncConcurrent(async e =>
            {
                if (e.New?.State == null)
                {
                    return;
                }

                await LightModeChangedAsync(e.New.State);
            });

            HouseModeChangedAsync(_currentHouseMode).Wait();
        }

        private async Task HouseModeChangedAsync(string? newMode)
        {
            if (newMode == null)
            {
                return;
            }

            _currentHouseMode = newMode;


            if (newMode == "DAG")
            {
                var state = _ha.Entity(_lightConfig.Config.ModeSensor).State;
                if (state != null)
                {
                    LightModeChangedAsync(state);
                }

                return;
            }

            foreach (var config in _lightConfig.Config.Data)
            {
                // We are at NIGHT or AWAY mode, turn off all configured lights.
                if (config.Modes.Any(m => m.Name == "AV"))
                {
                    await _z2MLightService.SetState(config, config.Modes.Single(m => m.Name == "AV"), true);
                }
            }
        }

        private async Task LightModeChangedAsync(string lightMode)
        {
            foreach (var config in _lightConfig.Config.Data)
            {
                if (config.Modes.Any(m => m.Name == lightMode))
                {
                    var mode = config.Modes.Single(m => m.Name == lightMode);
                    await _z2MLightService.SetState(config, config.Modes.Single(m=>m.Name == lightMode));
                }
            }
        }

    }
}
