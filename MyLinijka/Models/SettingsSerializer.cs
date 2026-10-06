using System.IO;
using System.Text.Json;

namespace MyLinijka.Models;

public static class SettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new();
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options)
            ?? throw new JsonException("Plik ustawień jest pusty.");
        settings.Validate();
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        settings.Validate();
        string json = JsonSerializer.Serialize(settings, Options);
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
