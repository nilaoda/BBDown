using System.IO;
using System.Text.Json;
using BBDown.GUI.Models;

namespace BBDown.GUI.Services;

public class ConfigService
{
    public static ConfigService Instance { get; } = new ConfigService();

    public AppConfig Config { get; private set; } = new();

    private readonly string _configPath;

    private ConfigService()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        else
        {
            Config = new AppConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
