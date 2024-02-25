using System.Text.Json.Serialization;

namespace MyNetDaemon.apps.MotionLights;

public class MotionSensorMessage
{
    [JsonPropertyName("battery")]
    public int Battery { get; set; }

    [JsonPropertyName("device_temperature")]
    public int DeviceTemperature { get; set; }

    [JsonPropertyName("illuminance")]
    public int Illuminance { get; set; }
    
    [JsonPropertyName("illuminance_lux")]
    public int IlluminanceLux { get; set; }

    [JsonPropertyName("last_seen")]
    public DateTime LastSeen { get; set; }

    [JsonPropertyName("linkquality")]
    public int LinkQuality { get; set; }

    [JsonPropertyName("occupancy")]
    public bool Occupancy { get; set; }

    [JsonPropertyName("power_outage_count")]
    public int PowerOutageCount { get; set; }

    [JsonPropertyName("voltage")]
    public int Voltage { get; set; }
}