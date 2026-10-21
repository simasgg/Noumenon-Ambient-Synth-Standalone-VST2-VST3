using System.Globalization;
using Noumenon.Engine.Parameters;
using Noumenon.Engine.Presets;

namespace Noumenon.Factory.Bank;

/// <summary>One factory preset as a recipe: where it is filed, its name and words, the seed its lanes (and its noise) come from, and the patch.</summary>
internal sealed record Recipe(string Family, string Name, ulong Seed, string Description, Action<PresetBuilder> Patch);

/// <summary>
/// The factory bank: the recipes of every family (Drone, Pad, Texture, Rhythmic, Broken),
/// written out as <c>content/presets/&lt;Family&gt;/&lt;Name&gt;.noumenon</c>, and the pass that
/// keeps the whole bank — the recipes and the presets made by hand before them — at one
/// loudness: each preset is auditioned (<see cref="Audition"/>) and its Output Volume moved until
/// it sits within half a LU of <see cref="TargetLufs"/>, or as close as the volume range allows.
/// A preset whose loudness the audition cannot read (silence) or whose output is not finite
/// fails the run. Everything is deterministic: the same recipes give byte-identical files.
/// </summary>
internal static partial class FactoryBank
{
    public const double TargetLufs = -20.0;
    private const double Tolerance = 0.5;
    private const int LevelPasses = 3;

    public static IReadOnlyList<Recipe> Recipes => [..Drones, ..Pads, ..Textures, ..Rhythms, ..Broken];

    public static IReadOnlyList<string> Write(string presetsFolder, string? only = null)
    {
        var written = new List<string>();
        foreach (var recipe in Recipes)
        {
            if (only is not null && !string.Equals(recipe.Name, only, StringComparison.OrdinalIgnoreCase))
                continue;

            var builder = new PresetBuilder(recipe.Seed);
            recipe.Patch(builder);
            var path = Path.Combine(presetsFolder, recipe.Family, recipe.Name + PresetIo.Extension);
            PresetIo.Save(builder.Build(recipe.Name, recipe.Description), path);
            written.Add(path);
        }

        return written;
    }

    /// <summary>Auditions the presets, moving each one's Output Volume towards the target when <paramref name="level"/> is set; prints a table and returns false if any preset failed.</summary>
    public static bool Check(IEnumerable<string> paths, string contentFolder, bool level)
    {
        var ok = true;
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine($"{"preset",-34} {"audition",-8} {"LUFS",7} {"peak",6} {"quiet",6} {"swing",6} {"centroid",9} {"volume",7}");
        foreach (var path in paths)
        {
            var state = PresetIo.Load(path);
            var kind = KindOf(state);
            var result = Audition.Measure(state, kind, contentFolder);
            var volume = state.Parameters.GetValueOrDefault(nameof(ParamId.OutputVolume), 0f);
            if (level && result.Finite && result.Lufs > -70.0)
            {
                var info = ParameterTable.Get(ParamId.OutputVolume);
                for (var pass = 0; pass < LevelPasses && Math.Abs(result.Lufs - TargetLufs) > Tolerance; pass++)
                {
                    var next = info.Clamp((float)Math.Round(volume + (TargetLufs - result.Lufs), 1));
                    if (next == volume)
                        break;

                    volume = next;
                    state.Parameters[nameof(ParamId.OutputVolume)] = volume;
                    result = Audition.Measure(state, kind, contentFolder);
                }

                PresetIo.Save(state, path);
            }

            var family = Path.GetFileName(Path.GetDirectoryName(path));
            var label = $"{family}/{state.Name}";
            var failed = !result.Finite || result.Lufs <= -70.0;
            var flag = failed ? "  FAILED" : Math.Abs(result.Lufs - TargetLufs) > 1.5 ? "  off-target" : "";
            ok &= !failed;
            Console.WriteLine($"{label,-34} {kind,-8} {result.Lufs.ToString("0.0", inv),7} {result.PeakDb.ToString("0.0", inv),6} {result.QuietestDb.ToString("0", inv),6} {result.SwingLu.ToString("0.0", inv),6} {result.CentroidHz.ToString("0", inv),9} {volume.ToString("+0.0;-0.0;0.0", inv),7}{flag}");
        }

        return ok;
    }

    /// <summary>How a preset has to be played to be heard: gated ones need notes, ones that listen to the input need audio.</summary>
    public static AuditionKind KindOf(PresetState state)
    {
        float Value(ParamId id) => state.Parameters.GetValueOrDefault(id.ToString(), ParameterTable.Get(id).Default);

        if (Value(ParamId.SamplerInputMix) >= 0.5f || ParameterTable.FollowerSources[(int)Value(ParamId.EnvFollowerSource)] is "Input")
            return AuditionKind.Input;

        return ParameterTable.PlayModes[(int)Value(ParamId.PlayMode)] == "Gated" ? AuditionKind.Notes : AuditionKind.Free;
    }
}
