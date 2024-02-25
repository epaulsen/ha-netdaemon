namespace MyNetDaemon.apps.Common;

internal record LightAttributes
{
    public int brightness { get; set; }

    public int BrightnessPercent => (int)Math.Round(brightness / 2.55, 0);
}