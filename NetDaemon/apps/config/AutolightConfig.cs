using System.Collections.Generic;

namespace MyNetDaemon.apps.config;

public class AutolightConfigService : YamlConfigurationBase<AutolightConfig>
{
    public AutolightConfigService(ILogger<AutolightConfigService> logger, string configPath) : base(logger, configPath) { }
}

public class AutolightConfig
{
    public string ModeSensor { get; set; } = "sensor.lyssensor";
    public string HouseModeSensor { get; set; }

    public List<LightConfig>? Data { get; set; } = new List<LightConfig>();
}

public class LightConfig
{
    public required string EntityId { get; set; }

    public string? MqttTopic { get; set; } = string.Empty;

    public List<StateData> Modes { get; set; } = new List<StateData>();
}

public class StateData
{
    public required string Name { get; set; }

    public string? Z2mData { get; set; }

    public int? BrightnessPercent { get; set; }

    public int? Transition { get; set; }

    public TimeSpan OverrideDelay { get; set; } = TimeSpan.FromHours(1);

    public bool Force { get; set; } = false;

    public bool TurnOff { get; set; } = false;
}