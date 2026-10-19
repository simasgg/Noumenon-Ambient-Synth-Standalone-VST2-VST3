using System.Globalization;
using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Samples;

namespace Noumenon.Factory.Samples;

/// <summary>
/// Renders the factory samples into <c>content/samples</c> and checks them the way the instrument
/// will read them: every file is decoded by the engine's own <see cref="SampleDecoder"/>, each
/// wavetable must come back as 32 frames, nothing may be silent, clipped, offset from zero or
/// carry a NaN, and a loop may not step from its last sample to its first any further than it
/// steps anywhere inside (no click at the wrap). Each file is finished the
/// same way — DC removed, peak normalized, 16-bit with seeded dither — so a re-run writes the same
/// bytes.
/// </summary>
internal static class SampleForge
{
    // A loop's wrap from its last sample to its first may step at most this much more than the largest step inside it.
    private const double SeamTolerance = 1.25;

    public static bool Render(string folder, string? only)
    {
        var ok = true;
        foreach (var spec in SampleCatalogue.All)
        {
            if (only is not null && !string.Equals(spec.Name, only, StringComparison.OrdinalIgnoreCase))
                continue;

            var buffer = spec.Render(spec.Seed);
            buffer.RemoveDc();
            buffer.Normalize(spec.PeakDb);
            WavWriter.Write(Path.Combine(folder, spec.RelativePath), buffer, spec.Seed * 0x9E3779B97F4A7C15UL);
        }

        ok &= List(folder, only);

        return ok;
    }

    /// <summary>Prints the decoded files with their figures; false when one fails a check.</summary>
    public static bool List(string folder, string? only = null)
    {
        var ok = true;
        var total = 0L;
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine($"{"sample",-34} {"ch",2} {"seconds",7} {"peak",6} {"rms",6} {"seam",6} {"KB",6}  check");
        foreach (var spec in SampleCatalogue.All)
        {
            if (only is not null && !string.Equals(spec.Name, only, StringComparison.OrdinalIgnoreCase))
                continue;

            var path = Path.Combine(folder, spec.RelativePath);
            if (!File.Exists(path))
            {
                Console.WriteLine($"{spec.RelativePath,-34} missing");
                ok = false;
                continue;
            }

            var data = SampleDecoder.Decode(path);
            var size = new FileInfo(path).Length;
            total += size;
            var problems = Check(spec, data, out var peakDb, out var rmsDb, out var seam);
            ok &= problems.Count == 0;
            Console.WriteLine($"{spec.RelativePath,-34} {(data.IsStereo ? 2 : 1),2} {data.Seconds.ToString("0.00", inv),7} {peakDb.ToString("0.0", inv),6} {rmsDb.ToString("0.0", inv),6} {seam.ToString("0.000", inv),6} {size / 1024,6}  {(problems.Count == 0 ? "ok" : string.Join("; ", problems))}");
        }

        Console.WriteLine($"{"total",-34} {"",2} {"",7} {"",6} {"",6} {"",6} {total / 1024,6}");

        return ok;
    }

    private static List<string> Check(SampleSpec spec, Dsp.Sampler.SampleData data, out double peakDb, out double rmsDb, out double seam)
    {
        var problems = new List<string>();
        var channels = data.IsStereo ? new[] { data.Left, data.Right } : new[] { data.Left };
        var peak = 0.0;
        var sum = 0.0;
        var finite = true;
        seam = 0.0;
        foreach (var channel in channels)
        {
            var mean = 0.0;
            foreach (var value in channel)
            {
                finite &= float.IsFinite(value);
                peak = Math.Max(peak, Math.Abs(value));
                sum += value * value;
                mean += value;
            }

            if (Math.Abs(mean / channel.Length) > 0.002)
                problems.Add("DC");

            var largestStep = 1e-9;
            for (var i = 1; i < channel.Length; i++)
                largestStep = Math.Max(largestStep, Math.Abs(channel[i] - channel[i - 1]));

            seam = Math.Max(seam, Math.Abs(channel[0] - channel[^1]) / largestStep);
        }

        peakDb = 20.0 * Math.Log10(Math.Max(peak, 1e-9));
        rmsDb = 10.0 * Math.Log10(Math.Max(sum / (data.Length * channels.Length), 1e-18));
        if (data.SampleRate != Synth.Rate)
            problems.Add($"rate {data.SampleRate}");

        if (!finite)
            problems.Add("NaN");

        if (peakDb > spec.PeakDb + 0.2 || peakDb < spec.PeakDb - 1.0)
            problems.Add($"peak {peakDb:0.0} dB");

        if (rmsDb < -60.0)
            problems.Add("silent");

        if (spec.Folder == "wavetables")
        {
            if (data.Length != Wavetable.FrameSize * Wavetable.MaxFrames)
                problems.Add($"{data.Length} frames");

            var frames = data.BuildWavetable().FrameCount;
            if (frames != Wavetable.MaxFrames)
                problems.Add($"{frames} table frames");
        }
        else if (spec.Loops && seam > SeamTolerance)
        {
            problems.Add($"seam {seam:0.000}");
        }

        return problems;
    }
}
