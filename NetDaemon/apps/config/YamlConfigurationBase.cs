using System.IO;
using System.IO.Compression;
using System.Reactive.Subjects;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyNetDaemon.apps.config;

public class YamlConfigurationBase<T> where T : class
{
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly PhysicalFileProvider _provider;
    private IChangeToken _changeToken;
    private bool loaded = false;

    private readonly Subject<T> _configChanges;



    public YamlConfigurationBase(ILogger logger, string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        _logger = logger;
        _configPath = configPath;
        _configChanges = new Subject<T>();
        var path = Path.IsPathRooted(_configPath)
            ? _configPath
            : Path.Combine(Directory.GetCurrentDirectory(), _configPath);


        _provider = new PhysicalFileProvider(Path.GetDirectoryName(path)!);

        Changed(null);
    }

    public async Task Changed(object? state)
    {
        var filename = Path.GetFileName(_configPath);
        if (!loaded)
        {
            _logger.LogInformation($"Loading config from '{filename}'");
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            _logger.LogInformation($"Reloading config from '{filename}'");
        }

        LoadConfig();

        _changeToken = _provider.Watch("*");
        _changeToken.RegisterChangeCallback(_ => Changed(null), null);
    }

    public T? Config { get; private set; }

    public IObservable<T> ConfigChanges => _configChanges;

    public IDisposable SubScribe(Action<T> callback)
    {
        var result = _configChanges.Subscribe(callback);
        if (Config != default)
        {
            _configChanges.OnNext(Config);
        }
        return result;
    }

    public IDisposable SubscribeAsync(Func<T, Task> callback)
    {
        var result = _configChanges.SubscribeAsync(callback);
        if (Config != default)
        {
            _configChanges.OnNext(Config);
        }

        return result;
    }


    private void LoadConfig()
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            using var reader = new StreamReader(_configPath, Encoding.UTF8);
            Config = deserializer.Deserialize<T>(reader);
            reader.Close();
            loaded = true;
            _configChanges.OnNext(Config);
        }
        catch (Exception e)
        {
            var yaml = File.ReadAllText(_configPath);
            _logger.LogError(e, $"Failed to read config from {_configPath}, yaml contents below:\n{yaml}");
        }
    }
}