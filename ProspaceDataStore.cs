using System.IO;
using System.Text.Json;

namespace Prospace;

/// <summary>Reads and writes Prospace's local, user-owned data file.</summary>
internal sealed class ProspaceDataStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Prospace",
        "data.json");

    public AppData Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppData>(File.ReadAllText(_path)) ?? new AppData();
        }
        catch (JsonException)
        {
            // Preserve the app's ability to start if the data file is malformed.
        }

        return new AppData();
    }

    public void Save(AppData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

