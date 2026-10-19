using System.Collections;
using System.Reflection;
using System.Runtime.Loader;

namespace Noumenon.Factory;

/// <summary>
/// Asks an installed plugin's own copy of the engine where its factory content is, the way the
/// plugin will ask inside a host: <c>Noumenon.Engine.dll</c> and its Noumenon dependencies are
/// loaded from the plugin's folder into a load context of their own (this tool holds its own copy
/// of the engine), and <c>FactoryContent.Locate</c> and a <c>PresetLibrary</c> run in it. The
/// installed plugin folders carry no content, so this proves the shared copy is found through the
/// installer's registry value. Run it from a folder with no content above it — the working
/// directory is the last place the lookup tries.
/// </summary>
internal static class InstalledContentProbe
{
    public static int Run(string pluginFolder, string? expected)
    {
        var context = new PluginContext(pluginFolder);
        try
        {
            var engine = context.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(pluginFolder, "Noumenon.Engine.dll")));
            var locate = engine.GetType("Noumenon.Engine.Presets.FactoryContent")!.GetMethod("Locate")!;
            var located = (string?)locate.Invoke(null, [Array.Empty<string?>()]);
            var library = Activator.CreateInstance(engine.GetType("Noumenon.Engine.Presets.PresetLibrary")!, [new string?[] { pluginFolder }])!;
            var factory = (ICollection)library.GetType().GetProperty("Factory")!.GetValue(library)!;
            Console.WriteLine($"Engine {engine.Location}");
            Console.WriteLine($"found  {located ?? "(no content)"}, {factory.Count} factory presets");
            if (located is null || factory.Count == 0)
                return 1;

            if (expected is not null && !string.Equals(Path.GetFullPath(located).TrimEnd('\\'), Path.GetFullPath(expected).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"expected {expected}");
                return 1;
            }

            return 0;
        }
        finally
        {
            context.Unload();
        }
    }

    private sealed class PluginContext(string folder) : AssemblyLoadContext("installed-plugin", isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName name)
        {
            var path = Path.Combine(folder, name.Name + ".dll");

            return name.Name?.StartsWith("Noumenon", StringComparison.Ordinal) == true && File.Exists(path) ? LoadFromAssemblyPath(Path.GetFullPath(path)) : null;
        }
    }
}
