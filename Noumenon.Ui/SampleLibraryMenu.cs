using System.Diagnostics;
using Noumenon.Engine.Samples;

namespace Noumenon.Ui;

/// <summary>
/// The sample library (<see cref="SampleLibrary"/>) as two submenus — Factory samples, with their
/// wavetables in a submenu of their own, and My samples, one submenu per subfolder and a way to
/// the folder — so any sample is one right-click away whatever preset is loaded, the bank an
/// installed Noumenon keeps under Program Files included. Picking one hands the entry to the
/// caller, which loads it through the host, off the UI thread, like any other file.
/// </summary>
internal static class SampleLibraryMenu
{
    /// <summary>Adds the two submenus over <paramref name="contentFolder"/> (null: the located factory content).</summary>
    public static void Add(ToolStripItemCollection items, string? contentFolder, Action<LibrarySample> load)
    {
        var entries = SampleLibrary.Scan(contentFolder);
        var factory = Submenu("Factory samples", entries.Where(e => e.IsFactory), load);
        if (factory.DropDownItems.Count > 0)
            items.Add(factory);

        var user = Submenu("My samples", entries.Where(e => !e.IsFactory), load);
        if (user.DropDownItems.Count == 0)
            user.DropDownItems.Add(new ToolStripMenuItem("(empty — put files in Documents\\Noumenon\\Samples)") { Enabled = false });

        user.DropDownItems.Add(new ToolStripSeparator());
        user.DropDownItems.Add("Open the folder", null, (_, _) => OpenUserFolder());
        items.Add(user);
    }

    /// <summary>Opens Documents\Noumenon\Samples in Explorer, creating it first.</summary>
    public static void OpenUserFolder()
    {
        Directory.CreateDirectory(SampleLibrary.UserFolder);
        Process.Start(new ProcessStartInfo(SampleLibrary.UserFolder) { UseShellExecute = true });
    }

    /// <summary>Opens Explorer on the folder holding <paramref name="file"/>, with the file selected.</summary>
    public static void ShowInFolder(string file)
    {
        if (File.Exists(file))
            Process.Start("explorer.exe", $"/select,\"{file}\"");
    }

    /// <summary>A heading for a library folder: FACTORY, FACTORY · WAVETABLES, MY SAMPLES, MY SAMPLES · DRUMS.</summary>
    public static string Heading(LibrarySample sample) =>
        (sample.IsFactory ? "FACTORY" : "MY SAMPLES") + (sample.Folder.Length == 0 ? "" : " · " + sample.Folder.Replace("/", " / ").ToUpperInvariant());

    private static ToolStripMenuItem Submenu(string caption, IEnumerable<LibrarySample> entries, Action<LibrarySample> load)
    {
        var menu = new ToolStripMenuItem(caption);
        var folders = new List<ToolStripMenuItem>();
        ToolStripMenuItem? folder = null;
        foreach (var entry in entries)
        {
            var item = new ToolStripMenuItem(entry.Name, null, (_, _) => load(entry));
            if (entry.Folder.Length == 0)
            {
                menu.DropDownItems.Add(item);
                continue;
            }

            if (folder is null || !string.Equals((string?)folder.Tag, entry.Folder, StringComparison.Ordinal))
            {
                folder = new ToolStripMenuItem(Capitalized(entry.Folder)) { Tag = entry.Folder };
                folders.Add(folder);
            }

            folder.DropDownItems.Add(item);
        }

        if (folders.Count > 0 && menu.DropDownItems.Count > 0)
            menu.DropDownItems.Add(new ToolStripSeparator());

        foreach (var sub in folders)
            menu.DropDownItems.Add(sub);

        return menu;
    }

    private static string Capitalized(string folder) => folder.Length == 0 ? folder : char.ToUpperInvariant(folder[0]) + folder[1..];
}
