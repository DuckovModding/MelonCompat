using AsmResolver.DotNet;
using MelonLoader;

namespace Bootstrap;

internal sealed class PluginMain : MelonPlugin
{
    public override void OnApplicationEarlyStart()
    {
        base.OnApplicationEarlyStart();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var steamAppsDirectory = Directory.GetParent(baseDirectory)?.Parent?.FullName;
        if (string.IsNullOrEmpty(steamAppsDirectory))
        {
            LoggerInstance.Warning("Could not determine SteamApps directory.");
            return;
        }

        var steamWorkshopDirectory = Path.Combine(steamAppsDirectory, "workshop", "content", "3167020");
        if (!Directory.Exists(steamWorkshopDirectory))
        {
            LoggerInstance.Warning($"Steam Workshop directory not found at {steamWorkshopDirectory}.");
            return;
        }

        var allLoadedMelons = new List<MelonBase>();
        LoadMelon(allLoadedMelons, steamWorkshopDirectory);
        LoadMelon(allLoadedMelons, Path.Combine(baseDirectory, "Duckov_Data", "Mods"));

        RegisterSorted(allLoadedMelons);
    }

    private void LoadMelon(List<MelonBase> melons, string folder)
    {
        foreach (var dllPath in Directory.EnumerateFiles(folder, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var assembly = AssemblyDefinition.FromFile(dllPath);
                var attribute = assembly.FindCustomAttributes("MelonLoader", "MelonInfoAttribute").FirstOrDefault();
                if (attribute == null)
                {
                    LoggerInstance.Msg($"Mod {Path.GetFileName(dllPath)} is not a MelonLoader mod. Skipping.");
                    continue;
                }

                var melonAssembly = MelonAssembly.LoadMelonAssembly(dllPath);
                LoggerInstance.Msg($"Loaded mod assembly from {dllPath}");

                melons.AddRange(melonAssembly.LoadedMelons);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to load mod assembly from {dllPath}: {ex.Message}");
            }
        }
    }
}