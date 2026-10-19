using Noumenon.Engine.Presets;

namespace Noumenon.Ui;

/// <summary>
/// The factory samples as a submenu, so the bank an installed Noumenon keeps under Program Files
/// is one right-click away instead of a folder hunt: an item per file in the factory content's
/// <c>samples</c> folder and the wavetables in a submenu of their own. Picking one hands its
/// portable factory reference (<see cref="FactoryContent.Sample"/>) to the caller, which loads it
/// through the host, off the UI thread, like any other file.
/// </summary>
internal static class FactorySampleMenu
{
    private const string WavetableFolder = "wavetables";

    /// <summary>The submenu over <paramref name="contentFolder"/> (null: <see cref="FactoryContent.Root"/>), or null when it has no samples. <paramref name="load"/> gets the reference and whether it is a wavetable.</summary>
    public static ToolStripMenuItem? Build(string caption, string? contentFolder, Action<string, bool> load)
    {
        if ((contentFolder ?? FactoryContent.Root) is not { } content)
            return null;

        var folder = Path.Combine(content, "samples");
        if (!Directory.Exists(folder))
            return null;

        var menu = new ToolStripMenuItem(caption);
        AddFiles(menu.DropDownItems, folder, "", false, load);
        var wavetables = Path.Combine(folder, WavetableFolder);
        if (Directory.Exists(wavetables))
        {
            var submenu = new ToolStripMenuItem("Wavetables");
            AddFiles(submenu.DropDownItems, wavetables, WavetableFolder + "/", true, load);
            if (submenu.DropDownItems.Count > 0)
            {
                menu.DropDownItems.Add(new ToolStripSeparator());
                menu.DropDownItems.Add(submenu);
            }
        }

        return menu.DropDownItems.Count > 0 ? menu : null;
    }

    private static void AddFiles(ToolStripItemCollection items, string folder, string prefix, bool wavetable, Action<string, bool> load)
    {
        var files = Directory.EnumerateFiles(folder, "*.wav").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var reference = FactoryContent.Sample(prefix + Path.GetFileName(file));
            items.Add(Path.GetFileNameWithoutExtension(file), null, (_, _) => load(reference, wavetable));
        }
    }
}
