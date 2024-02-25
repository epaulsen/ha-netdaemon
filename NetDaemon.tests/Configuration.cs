using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNetDaemon.apps.config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetDaemon.tests;

public class Configuration
{
    [Fact]
    public void ConfigReaderTest()
    {
        var sp = GetServiceProvider();

        var config = sp.GetService<AutolightConfigService>();
        config.Config.HouseModeSensor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateMotionConfigForKitchen()
    {
                var config = new MotionLightConfig();
                config.Zones.Add("Kitchen", new MotionZoneConfig
                {
                    Sensors = new() { "zigbee2mqtt/bevegelse kjøkken" },

                    Lights = new List<MotionLight>()
                    {
                        new MotionLight
                        {
                            Name = "zigbee2mqtt/Benkelys kjøkken",
                            Modes = new List<MotionLightState>
                            {
                                new MotionLightState
                                {
                                    Name = "NATT",
                                    OnState = "{ \"brightness_step\": 40,\"transition\":2}",
                                    OffState = "{ \"brightness_step\": -40,\"transition\":2}",
                                },

                                new MotionLightState
                                {
                                    Name = "DAG",
                                    OnState = "{ \"brightness_step\": 40,\"transition\":2}",
                                    OffState = "{ \"brightness_step\": -40,\"transition\":2}",
                                }

                            }
                        },
                        new MotionLight
                        {
                        Name = "zigbee2mqtt/Dimmer kjøkken",
                        Modes = new List<MotionLightState>
                        {
                            new MotionLightState
                            {
                                Name = "NATT",
                                OnState = "{ \"brightness_step\": 40,\"transition\":2}",
                                OffState = "{ \"brightness_step\": -40,\"transition\":2}",
                            },
                            new MotionLightState
                            {
                                Name = "DAG",
                                OnState = "{ \"brightness_step\": 40,\"transition\":2}",
                                OffState = "{ \"brightness_step\": -40,\"transition\":2}",
                            }
                        }
                    }
                    }
                });

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(config);
        yaml.Should().NotBeNullOrEmpty();
    }


    [Fact]
    public async Task ConfigReaderTest_Reload()
    {
        var sp = GetServiceProvider();
        var config = sp.GetService<AutolightConfigService>();
        config.Config.HouseModeSensor.Should().NotBeNullOrEmpty();

        string text = @"
- EntityId: light.trapp
  MqttTopic: ''
  Modes:
  - Name: DAG
    Z2mData: 
    BrightnessPercent: 1
    Transition: 
    OverrideDelay: 01:00:00
    Force: false
  - Name: SKUMRING
    Z2mData: 
    BrightnessPercent: 50
    Transition: 
    OverrideDelay: 01:00:00
    Force: false
  - Name: NATT
    Z2mData: 
    BrightnessPercent: 25
    Transition: 
    OverrideDelay: 01:00:00
    Force: false";

        await File.AppendAllTextAsync("./apps/config/autolights.yaml", text, Encoding.UTF8);

        await Task.Delay(TimeSpan.FromSeconds(3));

        config.Config.Data.Any(x => x.EntityId == "light.trapp").Should().BeTrue();
    }

    private IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddHttpClient();
        services.AddConfigService<AutolightConfigService,AutolightConfig>("autolights.yaml");
        return services.BuildServiceProvider();
    }
}