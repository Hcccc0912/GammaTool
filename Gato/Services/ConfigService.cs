using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gato.Models;

namespace Gato.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.Strict
    };

    private readonly string _configPath;

    public ConfigService() : this(Path.Combine(AppContext.BaseDirectory, "config.json")) { }

    public ConfigService(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaultConfig = AppConfig.CreateDefault();
            Save(defaultConfig);
            return defaultConfig;
        }

        var json = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
            ?? throw new JsonException("The configuration file is empty or could not be parsed.");

        var migrated = false;
        foreach (var profile in config.Profiles)
        {
            var englishName = profile.Name switch
            {
                "\u9ED8\u8BA4" => "Default",
                "\u65B0\u914D\u7F6E" => "New Profile",
                _ => profile.Name
            };
            if (englishName == profile.Name) continue;
            profile.Name = englishName;
            migrated = true;
        }

        if (migrated) Save(config);
        return config;
    }

    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
