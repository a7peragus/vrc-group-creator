using Newtonsoft.Json;
using System.IO;
using VrcGroupCreator.Models;

namespace VrcGroupCreator.Services;

public class SettingsService
{
    private readonly string _filePath;
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "VrcGroupCreator");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        
        Load();
    }

    public void Load()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch { Settings = new AppSettings(); }
        }
        else
        {
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
