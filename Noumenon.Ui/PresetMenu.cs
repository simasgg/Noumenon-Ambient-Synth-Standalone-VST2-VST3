using Noumenon.Engine.Presets;

namespace Noumenon.Ui;

/// <summary>
/// The preset bank as every host's lists and menus show it: one flat list of names, in the order
/// the ◀ ▶ buttons step through (the factory bank, then the user's), and beside it the group each
/// one is shown in — its factory family (Drone, Pad, …), nothing for the factory bank's root, and
/// <c>User</c> for the user bank, with the user's own subfolders under it. The editor's preset
/// menu is built from the pair: the ungrouped presets at the top, then one submenu per group,
/// nested at every '/', with the current preset and the submenus leading to it checked. Where a
/// single name stands alone, outside the menu, <see cref="Label"/> marks a factory preset with a
/// star, since a user preset saved from it usually keeps its name.
/// </summary>
public static class PresetMenu
{
    public const string UserGroup = "User";

    public static string[] Names(PresetLibrary library) => library.All.Select(preset => preset.Name).ToArray();

    public static string[] Groups(PresetLibrary library) =>
    [
        ..library.Factory.Select(preset => preset.Family),
        ..library.User.Select(preset => preset.Family.Length == 0 ? UserGroup : UserGroup + "/" + preset.Family),
    ];

    public static bool IsUser(string group) => group == UserGroup || group.StartsWith(UserGroup + "/", StringComparison.Ordinal);

    public static string Label(string name, string group) => IsUser(group) ? name : "★ " + name;

    /// <summary><paramref name="pick"/> receives the chosen preset's index.</summary>
    public static void Fill(ToolStripItemCollection items, IReadOnlyList<string> names, IReadOnlyList<string> groups, int current, Action<int> pick)
    {
        var grouped = new List<int>();
        for (var i = 0; i < names.Count; i++)
        {
            if (GroupOf(groups, i).Length == 0)
                items.Add(Item(names[i], i, current, pick));
            else
                grouped.Add(i);
        }

        if (grouped.Count > 0 && items.Count > 0)
            items.Add(new ToolStripSeparator());

        var submenus = new Dictionary<string, ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in grouped)
        {
            var path = GroupOf(groups, i);
            var parent = Submenu(items, submenus, path);
            parent.DropDownItems.Add(Item(names[i], i, current, pick));
            if (i != current)
                continue;

            for (var key = path; key.Length > 0; key = key.Contains('/') ? key[..key.LastIndexOf('/')] : "")
                submenus[key].Checked = true;
        }
    }

    private static string GroupOf(IReadOnlyList<string> groups, int index) => index < groups.Count ? groups[index].Trim('/') : "";

    private static ToolStripMenuItem Submenu(ToolStripItemCollection top, Dictionary<string, ToolStripMenuItem> submenus, string path)
    {
        if (submenus.TryGetValue(path, out var existing))
            return existing;

        var slash = path.LastIndexOf('/');
        var items = slash < 0 ? top : Submenu(top, submenus, path[..slash]).DropDownItems;
        var submenu = new ToolStripMenuItem(slash < 0 ? path : path[(slash + 1)..]);
        if (submenu.DropDown is ToolStripDropDownMenu dropDown)
        {
            dropDown.ShowCheckMargin = true;
            dropDown.ShowImageMargin = false;
        }

        items.Add(submenu);
        submenus[path] = submenu;

        return submenu;
    }

    private static ToolStripMenuItem Item(string name, int index, int current, Action<int> pick)
    {
        var item = new ToolStripMenuItem(name) { Checked = index == current };
        item.Click += (_, _) => pick(index);

        return item;
    }
}
