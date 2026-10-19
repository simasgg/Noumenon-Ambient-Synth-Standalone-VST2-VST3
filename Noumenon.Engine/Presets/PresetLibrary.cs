namespace Noumenon.Engine.Presets;

/// <summary>
/// The two preset folders every host browses: the factory bank — the <c>presets</c> folder of the
/// factory content (<see cref="FactoryContent"/>: next to the executable or plugin, up the tree
/// from a build output, or where the installer put it) — and the user's own bank in their
/// Documents folder, where Save As defaults to, shared by the standalone and the plugins so a
/// snapshot saved in one is in the other's list. Either bank may have subfolders, which are
/// families (the factory bank's Drone, Pad, Texture, …): every preset knows its family, and each
/// bank lists its root first, then family by family. A bare name on the command line, or a
/// <c>Family/Name</c>, is looked up in both banks. A host that is not the application (a plugin
/// inside a DAW, whose base directory is the DAW's) passes the folder its assembly was loaded from
/// as a starting point.
/// </summary>
public sealed class PresetLibrary
{
    public PresetLibrary(params string?[] startFolders)
    {
        ContentFolder = FactoryContent.Locate(startFolders);
        FactoryFolder = ContentFolder is null ? null : Path.Combine(ContentFolder, "presets");
        UserFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Noumenon", "Presets");
        Rescan();
    }

    public string? ContentFolder { get; }

    public string? FactoryFolder { get; }

    public string UserFolder { get; }

    public IReadOnlyList<PresetInfo> Factory { get; private set; } = [];

    public IReadOnlyList<PresetInfo> User { get; private set; } = [];

    public IReadOnlyList<PresetInfo> All { get; private set; } = [];

    public void Rescan()
    {
        Factory = FactoryFolder is null ? [] : PresetIo.Scan(FactoryFolder);
        User = PresetIo.Scan(UserFolder);
        All = [..Factory, ..User];
    }

    public int IndexOf(string? path)
    {
        if (path is null)
            return -1;

        for (var i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i].Path, path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public bool IsFactory(string? path) => path is not null && FactoryFolder is not null && path.StartsWith(FactoryFolder, StringComparison.OrdinalIgnoreCase);

    /// <summary>A path as given, or a preset name (or <c>Family/Name</c>) looked up in the factory bank then the user bank; null when neither has it.</summary>
    public string? Find(string nameOrPath)
    {
        if (File.Exists(nameOrPath))
            return Path.GetFullPath(nameOrPath);

        var name = nameOrPath.EndsWith(PresetIo.Extension, StringComparison.OrdinalIgnoreCase) ? nameOrPath[..^PresetIo.Extension.Length] : nameOrPath;
        name = name.Replace('\\', '/');
        foreach (var preset in All)
        {
            if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(preset.Family + "/" + preset.Name, name, StringComparison.OrdinalIgnoreCase))
                return preset.Path;
        }

        return null;
    }

    public static string? Resolve(string nameOrPath, params string?[] startFolders) => new PresetLibrary(startFolders).Find(nameOrPath);
}
