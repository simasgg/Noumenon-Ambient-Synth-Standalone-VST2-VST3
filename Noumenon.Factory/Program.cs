using System.Drawing.Imaging;
using Noumenon.Engine.Presets;
using Noumenon.Factory.Artwork;
using Noumenon.Factory.Bank;
using Noumenon.Factory.Samples;

// The factory content, built from code: samples, the preset bank, the artwork.
//   Usage: Noumenon.Factory <command> [options]
//     samples [--only <name>] [--list]   render content/samples (or only list and check them)
//     presets [--only <name>]            write the recipes into content/presets, then level every factory preset
//     check                              audition every factory preset and report, writing nothing
//     artwork                            draw the icon and the installer's images
//     all                                samples, presets and artwork, in that order
// The repository is found up the tree from the working directory (the folder holding Noumenon.slnx).
var command = args.Length > 0 ? args[0].ToLowerInvariant() : "";
string? only = null;
var listOnly = false;
for (var i = 1; i < args.Length; i++)
{
    if (args[i].Equals("--only", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        only = args[++i];
    else if (args[i].Equals("--list", StringComparison.OrdinalIgnoreCase))
        listOnly = true;
    else
    {
        Console.Error.WriteLine($"Unknown option '{args[i]}'.");
        return 2;
    }
}

var root = FindRepository();
if (root is null)
{
    Console.Error.WriteLine("Run this inside the Noumenon repository (no Noumenon.slnx found up the tree).");
    return 2;
}

var samples = Path.Combine(root, "content", "samples");
var presets = Path.Combine(root, "content", "presets");

return command switch
{
    "samples" => listOnly ? Exit(SampleForge.List(samples, only)) : Exit(SampleForge.Render(samples, only)),
    "presets" => Exit(BuildPresets()),
    "check" => Exit(FactoryBank.Check(PresetIo.Scan(presets).Select(preset => preset.Path), level: false)),
    "artwork" => Exit(DrawArtwork()),
    "all" => Exit(SampleForge.Render(samples, null) && BuildPresets() && DrawArtwork()),
    _ => Usage(),
};

bool BuildPresets()
{
    var written = FactoryBank.Write(presets, only);
    Console.WriteLine($"Wrote {written.Count} recipe presets; levelling {(only is null ? "the whole bank" : only)} to {FactoryBank.TargetLufs} LUFS.");
    var targets = only is null ? PresetIo.Scan(presets).Select(preset => preset.Path).ToList() : written.ToList();

    return FactoryBank.Check(targets, level: true);
}

bool DrawArtwork()
{
    int[] iconSizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];
    var icons = iconSizes.Select(ArtworkRenderer.Icon).ToList();
    var iconPath = Path.Combine(root, "Noumenon.Standalone", "Noumenon.ico");
    IconFile.Write(iconPath, icons);
    icons[^1].Save(Path.Combine(root, "docs", "icon.png"), ImageFormat.Png);
    foreach (var icon in icons)
        icon.Dispose();

    var installer = Path.Combine(root, "installer");
    foreach (var (width, height, suffix) in new[] { (164, 314, ""), (246, 459, "-150"), (328, 628, "-200") })
    {
        using var image = ArtworkRenderer.WizardImage(width, height);
        image.Save(Path.Combine(installer, $"WizardImage{suffix}.bmp"), ImageFormat.Bmp);
    }

    foreach (var (width, height, suffix) in new[] { (55, 55, ""), (83, 80, "-150"), (110, 106, "-200") })
    {
        using var image = ArtworkRenderer.WizardSmallImage(width, height);
        image.Save(Path.Combine(installer, $"WizardSmallImage{suffix}.bmp"), ImageFormat.Bmp);
    }

    Console.WriteLine($"Artwork: {iconPath}, docs\\icon.png and the wizard images in {installer}.");

    return true;
}

static string? FindRepository()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Noumenon.slnx")))
                return dir.FullName;
        }
    }

    return null;
}

static int Exit(bool ok) => ok ? 0 : 1;

static int Usage()
{
    Console.Error.WriteLine("Usage: Noumenon.Factory samples [--only <name>] [--list] | presets [--only <name>] | check | artwork | all");

    return 2;
}
