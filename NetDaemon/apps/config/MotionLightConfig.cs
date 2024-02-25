using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyNetDaemon.apps.config;
public class MotionLightConfig
{
    public Dictionary<string, MotionZoneConfig> Zones { get; set; } = new();
}

public class MotionZoneConfig
{
    public List<string> Sensors { get; set; } = new();
    
    public List<MotionLight> Lights { get; set; } = new();

    public TimeSpan OccupancyTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class MotionLight
{
    public string Name { get; set; }
    public List<MotionLightState> Modes { get; set; } = new();
}

public class MotionLightState
{
    public string Name { get; set; }
    public string OnState { get; set; }
    public string OffState { get; set; }
    
}
