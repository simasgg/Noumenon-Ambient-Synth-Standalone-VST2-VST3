using Microsoft.Win32;

namespace Noumenon.Engine.Presets;

/// <summary>
/// Where the factory content lives — the <c>content</c> folder holding the preset bank
/// (<c>presets</c>, one subfolder per family) and the samples (<c>samples</c>) — and how a
/// snapshot names a factory sample so it loads on any machine and in any host.
///
/// The folder is looked for up the tree from the given start folders, from where this assembly
/// was loaded, from the application's base directory and from the working directory, so a build
/// output finds the repository's content and a portable plugin folder finds the copy it carries;
/// then where the installer put it, which it records in the registry
/// (<c>SOFTWARE\Noumenon</c>, value <c>ContentFolder</c>, under HKLM for a machine-wide install,
/// else HKCU). The installer keeps one copy for the standalone and both plugin formats, because
/// the plugins sit in the shared VST folders, far from it.
///
/// A factory sample is referenced as <c>factory:samples/Name.wav</c>, relative to the content
/// folder, instead of by its absolute path: the standalone, the VST2 and the VST3 plugin each see
/// the content at a different path, and so does another machine. Loading a sample from inside the
/// content folder by its plain path turns it into that form too (<see cref="ToReference"/>), so
/// every snapshot saved anywhere stays portable.
/// </summary>
public static class FactoryContent
{
    public const string Scheme = "factory:";
    public const string RegistryKey = @"SOFTWARE\Noumenon";
    public const string RegistryValue = "ContentFolder";

    private const int SearchDepth = 8;

    private static readonly Lazy<string?> root = new(() => Locate());

    /// <summary>The content folder this process uses (located once, from this assembly's folder), or null when there is none.</summary>
    public static string? Root => root.Value;

    public static string? SamplesFolder => Root is { } folder ? Path.Combine(folder, "samples") : null;

    /// <summary>The first <c>content</c> folder holding a <c>presets</c> folder, walking up from each start folder, then this assembly's, the base and the working directory; then the installed one; null when none exists.</summary>
    public static string? Locate(params string?[] startFolders)
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(FactoryContent).Assembly.Location);
        foreach (var start in startFolders.Concat([assemblyFolder, AppContext.BaseDirectory, Directory.GetCurrentDirectory()]))
        {
            if (string.IsNullOrEmpty(start))
                continue;

            var dir = new DirectoryInfo(start);
            for (var depth = 0; depth < SearchDepth && dir is not null; depth++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "content");
                if (Directory.Exists(Path.Combine(candidate, "presets")))
                    return candidate;
            }
        }

        return Installed();
    }

    public static string? Installed()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            try
            {
                using var key = hive.OpenSubKey(RegistryKey);
                if (key?.GetValue(RegistryValue) is string folder && Directory.Exists(Path.Combine(folder, "presets")))
                    return folder;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
            }
        }

        return null;
    }

    public static bool IsReference(string path) => path.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase);

    /// <summary>The file a sample reference names: a factory reference resolved against <paramref name="contentFolder"/> (default <see cref="Root"/>), any other path unchanged.</summary>
    public static string Resolve(string reference, string? contentFolder = null)
    {
        if (!IsReference(reference))
            return reference;

        var folder = contentFolder ?? Root;
        var relative = reference[Scheme.Length..].Replace('/', Path.DirectorySeparatorChar);
        if (folder is null)
            throw new FileNotFoundException($"The factory content folder was not found, so {reference} cannot be loaded.", reference);

        return Path.Combine(folder, relative);
    }

    /// <summary>The portable name of a sample file: a path inside <paramref name="contentFolder"/> (default <see cref="Root"/>) as a factory reference, anything else unchanged.</summary>
    public static string ToReference(string path, string? contentFolder = null)
    {
        var folder = contentFolder ?? Root;
        if (IsReference(path) || folder is null || !Path.IsPathFullyQualified(path))
            return path;

        var relative = Path.GetRelativePath(folder, path);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathFullyQualified(relative))
            return path;

        return Scheme + relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>The factory reference for a file under the content folder, e.g. <c>Sample("Wind.wav")</c> is <c>factory:samples/Wind.wav</c>.</summary>
    public static string Sample(string relativeToSamples) => Scheme + "samples/" + relativeToSamples.Replace('\\', '/');
}
