using Noumenon.Engine.Presets;

namespace Noumenon.Engine.Samples;

/// <summary>
/// Every sample a player can reach whatever preset is loaded: the factory samples (the content
/// folder's <c>samples</c>) and the player's own folder, <c>Documents\Noumenon\Samples</c>, shared
/// by every host like the user preset bank. A preset still carries the samples it plays and a new
/// one replaces them; the library is what keeps the rest one click away. Subfolders are groups,
/// as the preset bank's subfolders are families, and a folder named <c>wavetables</c> holds
/// wavetables in either place. Factory entries are portable <c>factory:</c> references, the
/// player's are plain paths. A scan reads directories only — it never decodes — skips hidden,
/// system and linked folders, and stops at <see cref="MaxEntries"/>, so it is cheap enough for the
/// UI thread.
/// </summary>
public static class SampleLibrary
{
    public const int MaxEntries = 2000;
    private const int MaxDepth = 4;
    private const string WavetableFolder = "wavetables";

    /// <summary>What the sample dialogs and the library accept; WAV and AIFF decode everywhere, the rest through the decoder a Windows host registers.</summary>
    public static readonly string[] AudioExtensions = [".wav", ".wave", ".aif", ".aiff", ".aifc", ".mp3", ".m4a", ".aac", ".mp4", ".wma", ".flac", ".ogg", ".opus"];

    private static readonly EnumerationOptions Listing = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
    };

    public static string UserFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Noumenon", "Samples");

    public static bool IsAudioFile(string path) => AudioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>The factory samples of <paramref name="contentFolder"/> (null: <see cref="FactoryContent.Root"/>), then the files of <paramref name="userFolder"/> (null: <see cref="UserFolder"/>); each folder's own files first, then its subfolders by name.</summary>
    public static IReadOnlyList<LibrarySample> Scan(string? contentFolder = null, string? userFolder = null)
    {
        var entries = new List<LibrarySample>();
        if ((contentFolder ?? FactoryContent.Root) is { } content)
            Add(entries, Path.Combine(content, "samples"), "", 0, true);

        Add(entries, userFolder ?? UserFolder, "", 0, false);

        return entries;
    }

    /// <summary>Copies a file into <paramref name="userFolder"/> (null: <see cref="UserFolder"/>), creating it, under a free name, and returns the copy's path; a file already in the folder is returned as it is.</summary>
    public static string AddToUserFolder(string path, string? userFolder = null)
    {
        var folder = Path.GetFullPath(userFolder ?? UserFolder);
        var source = Path.GetFullPath(path);
        if (source.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return source;

        Directory.CreateDirectory(folder);
        var name = Path.GetFileNameWithoutExtension(source);
        var extension = Path.GetExtension(source);
        var target = Path.Combine(folder, name + extension);
        for (var n = 2; File.Exists(target); n++)
            target = Path.Combine(folder, $"{name} ({n}){extension}");

        File.Copy(source, target);

        return target;
    }

    private static void Add(List<LibrarySample> entries, string folder, string relative, int depth, bool factory)
    {
        if (!Directory.Exists(folder))
            return;

        try
        {
            var wavetables = relative.Split('/').Contains(WavetableFolder, StringComparer.OrdinalIgnoreCase);
            var files = Directory.EnumerateFiles(folder, "*", Listing).Where(IsAudioFile).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                if (entries.Count >= MaxEntries)
                    return;

                var name = Path.GetFileName(file);
                var path = factory ? FactoryContent.Sample(relative.Length == 0 ? name : relative + "/" + name) : file;
                entries.Add(new LibrarySample(factory, relative, Path.GetFileNameWithoutExtension(file), path, wavetables));
            }

            if (depth >= MaxDepth)
                return;

            foreach (var sub in Directory.EnumerateDirectories(folder, "*", Listing).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                Add(entries, sub, relative.Length == 0 ? Path.GetFileName(sub) : relative + "/" + Path.GetFileName(sub), depth + 1, factory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }
    }
}

/// <summary>One sample of the <see cref="SampleLibrary"/>.</summary>
/// <param name="IsFactory">A factory sample (its <paramref name="Path"/> a <c>factory:</c> reference) rather than one of the player's.</param>
/// <param name="Folder">The subfolder it is in, relative to its library's root with <c>/</c> separators; empty at the root.</param>
/// <param name="Name">The file name without its extension.</param>
/// <param name="Path">What the sample map loads: a factory reference or a full path.</param>
/// <param name="IsWavetable">It is in a <c>wavetables</c> folder, so it is meant for the oscillators' Wavetable wave.</param>
public sealed record LibrarySample(bool IsFactory, string Folder, string Name, string Path, bool IsWavetable);
