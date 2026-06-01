using System.IO;
using System.Text.Json;
using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public static class MaskSettingsStore
{
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    private static MaskSettings? CachedSettings;

    public static event EventHandler? SettingsChanged;

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrivacyMasker");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static MaskSettings Load()
    {
        lock (SyncRoot)
        {
            if (CachedSettings is not null)
            {
                return Copy(CachedSettings);
            }

            try
            {
                CachedSettings = File.Exists(SettingsPath)
                    ? JsonSerializer.Deserialize<MaskSettings>(File.ReadAllText(SettingsPath)) ?? new MaskSettings()
                    : new MaskSettings();
            }
            catch
            {
                CachedSettings = new MaskSettings();
            }

            return Copy(CachedSettings);
        }
    }

    public static void Save(MaskSettings settings)
    {
        lock (SyncRoot)
        {
            CachedSettings = Copy(settings);
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }

        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    private static MaskSettings Copy(MaskSettings settings)
    {
        return new MaskSettings
        {
            MaskKind = settings.MaskKind,
            PresetId = settings.PresetId,
            Message = settings.Message,
            AssetPath = settings.AssetPath,
            Opacity = settings.Opacity
        };
    }
}
