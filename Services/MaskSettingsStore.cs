using System.IO;
using System.Text.Json;
using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public static class MaskSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static event EventHandler? SettingsChanged;

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrivacyMasker");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static MaskSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new MaskSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<MaskSettings>(json) ?? new MaskSettings();
        }
        catch
        {
            return new MaskSettings();
        }
    }

    public static void Save(MaskSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }
}
